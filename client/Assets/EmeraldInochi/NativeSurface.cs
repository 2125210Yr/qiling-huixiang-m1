using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Inochi2D.Internal;

// This adapter consumes the official core's deformed draw list. It does not animate vertices.
// Pinned 0.8 ABI only; albedo, mask and the blend modes used by Aka are validated.
internal sealed class NativeSurface : IDisposable {
    public RenderTexture Output { get; private set; }
    readonly InPuppet puppet;
    readonly List<Texture2D> textures = new();
    readonly List<RenderTexture> composites = new();
    readonly Dictionary<InBlendMode, Material> materials = new();
    readonly CommandBuffer commands = new() { name = "Inochi native draw list to surface" };
    readonly Mesh mesh = new() { name = "Native deformed mesh" };
    readonly RenderTexture mask;
    readonly Matrix4x4 projection;
    readonly Material maskMaterial;
    int vertexCount=-1,indexCount=-1;
    bool disposed;
    readonly VertexAttributeDescriptor[] layout = {
        new(VertexAttribute.Position, VertexAttributeFormat.Float32,2),
        new(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32,2)
    };
    public NativeSurface(InPuppet source, int width=900, int height=1000) {
        puppet=source;
        try {
        foreach(var original in source.Textures) {
            var texture=original; texture.ID=textures.Count;
            textures.Add(texture.ToTexture2D());
        }
        Output=Target(width,height,"Character albedo");
        mask=Target(width,height,"Character mask");
        maskMaterial=new Material(Shader.Find("EmeraldInochi/Native"));
        maskMaterial.SetInt("_Src",(int)BlendMode.One);
        maskMaterial.SetInt("_Dst",(int)BlendMode.One);
        maskMaterial.SetInt("_DstAlpha",(int)BlendMode.One);
        source.Update(0);
        var min=new Vector2(float.MaxValue,float.MaxValue); var max=-min;
        foreach(var vertex in source.DrawList.VertexData) {
            // Composite screen-space rectangles are [-1,1] and do not affect the model bounds.
            var position=new Vector2(vertex.vtx.x,-vertex.vtx.y);
            min=Vector2.Min(min,position); max=Vector2.Max(max,position);
        }
        var center=(min+max)*.5f;
        float halfHeight=Mathf.Max((max.y-min.y)*.55f,(max.x-min.x)*.55f*height/width);
        float halfWidth=halfHeight*width/height;
        projection=GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-512,512,-768,768,-1,1),true);
        mesh.MarkDynamic();
        } catch {Dispose();throw;}
    }
    static RenderTexture Target(int w,int h,string name) {
        var target=new RenderTexture(w,h,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear) {name=name,filterMode=FilterMode.Bilinear};
        target.Create(); return target;
    }
    Material GetMaterial(InBlendMode mode) {
        if(materials.TryGetValue(mode,out var material))return material;
        material=new Material(Shader.Find("EmeraldInochi/Native"));
        int src,dst;
        switch(mode) {
            case InBlendMode.Normal: src=(int)BlendMode.One;dst=(int)BlendMode.OneMinusSrcAlpha;break;
            case InBlendMode.Multiply:src=(int)BlendMode.DstColor;dst=(int)BlendMode.OneMinusSrcAlpha;break;
            // Inochi calls this SourceIn, but defines it as ClipToLower (preserve the lower layer).
            case InBlendMode.SourceIn:src=(int)BlendMode.DstAlpha;dst=(int)BlendMode.OneMinusSrcAlpha;break;
            default: throw new NotSupportedException("Unverified Inochi blend mode: "+mode);
        }
        material.SetInt("_Src",src);material.SetInt("_Dst",dst);
        material.SetInt("_SrcAlpha",mode==InBlendMode.SourceIn?(int)BlendMode.DstAlpha:(int)BlendMode.One);
        material.SetInt("_DstAlpha",(int)BlendMode.OneMinusSrcAlpha);
        materials.Add(mode,material);return material;
    }
    public unsafe void Render(bool bypassMasks=false) {
        var draw=puppet.DrawList;var vertices=draw.VertexData;var indices=draw.IndexData;var allocations=draw.Allocations;
        if(vertexCount!=vertices.Length||indexCount!=indices.Length) {
            mesh.Clear();vertexCount=vertices.Length;indexCount=indices.Length;
            mesh.SetVertexBufferParams(vertices.Length,layout);
            mesh.SetIndexBufferParams(indices.Length,IndexFormat.UInt32);
            mesh.SetIndexBufferData(indices,0,0,indices.Length);
            var descriptions=new SubMeshDescriptor[allocations.Length];
            for(int i=0;i<allocations.Length;i++) {
                var a=allocations[i];
                descriptions[i]=new SubMeshDescriptor((int)a.IdxOffset,(int)a.IdxCount){baseVertex=(int)a.VtxOffset,vertexCount=(int)a.VtxCount};
            }
            mesh.SetSubMeshes(descriptions,MeshUpdateFlags.DontRecalculateBounds);
        }
        mesh.SetVertexBufferData(vertices,0,0,vertices.Length);
        // The arrays above borrow native memory; they must not be disposed here.
        commands.Clear();commands.SetRenderTarget(Output);commands.ClearRenderTarget(false,true,Color.clear);
        commands.SetGlobalMatrix("_NativeProjection",projection);
        commands.SetGlobalMatrix("_NativeClip",GL.GetGPUProjectionMatrix(Matrix4x4.identity,true));
        commands.SetGlobalTexture("_NativeMask",mask);
        int depth=0; bool defining=false; bool compositeMasked=false;
        RenderTexture Current()=>depth==0?Output:composites[depth-1];
        foreach(var original in draw.Commands) {
            var cmd=original;
            if(cmd.State==DrawState.CompositeBegin) {
                if(depth==composites.Count)composites.Add(Target(Output.width,Output.height,"Composite "+depth));
                depth++;commands.SetRenderTarget(Current());commands.ClearRenderTarget(false,true,Color.clear);defining=false;continue;
            }
            if(cmd.State==DrawState.CompositeEnd) {depth--;if(depth<0)throw new Exception("Unbalanced composite");commands.SetRenderTarget(Current());defining=false;compositeMasked=false;continue;}
            if(cmd.State==DrawState.DefineMask) {
                if(cmd.MaskMode!=MaskMode.Mask)throw new NotSupportedException("Dodge mask not verified");
                if(!defining){commands.SetRenderTarget(mask);commands.ClearRenderTarget(false,true,Color.clear);defining=true;}
                compositeMasked=true;
                commands.SetGlobalTexture("_NativeTexture",textures[cmd.Sources[0].ID]);
                commands.DrawMesh(mesh,Matrix4x4.identity,maskMaterial,(int)cmd.AllocId,1);
                continue;
            }
            commands.SetRenderTarget(Current());defining=false;
            // 0.8 native PartVars / CompositeVars have fields aligned to 16 bytes.
            float* data=(float*)cmd.Data;
            commands.SetGlobalVector("_NativeTint",new Vector4(data[0],data[1],data[2],1));
            commands.SetGlobalVector("_NativeScreen",new Vector4(data[4],data[5],data[6],0));
            commands.SetGlobalFloat("_NativeOpacity",data[8]);
            bool clipped=cmd.State==DrawState.MaskedDraw || (cmd.State==DrawState.CompositeBlit && compositeMasked);
            commands.SetGlobalFloat("_NativeClipped",clipped&&!bypassMasks?1:0);
            var material=GetMaterial(cmd.BlendMode);
            if(cmd.State==DrawState.CompositeBlit) {
                commands.SetGlobalTexture("_NativeTexture",composites[depth]);
                commands.DrawMesh(mesh,Matrix4x4.identity,material,(int)cmd.AllocId,2);
                compositeMasked=false;
            } else if(cmd.State==DrawState.Normal || cmd.State==DrawState.MaskedDraw) {
                commands.SetGlobalTexture("_NativeTexture",textures[cmd.Sources[0].ID]);
                commands.DrawMesh(mesh,Matrix4x4.identity,material,(int)cmd.AllocId,0);
                compositeMasked=false;
            } else throw new NotSupportedException("Unknown draw state "+cmd.State);
        }
        if(depth!=0)throw new Exception("Unclosed native composite");
        Graphics.ExecuteCommandBuffer(commands);
    }
    public void Dispose() {
        if(disposed)return;disposed=true;
        commands.Dispose();UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(maskMaterial);
        foreach(var texture in textures)UnityEngine.Object.Destroy(texture);
        foreach(var material in materials.Values)UnityEngine.Object.Destroy(material);
        foreach(var target in composites){target.Release();UnityEngine.Object.Destroy(target);}
        if(mask!=null){mask.Release();UnityEngine.Object.Destroy(mask);}
        if(Output!=null){Output.Release();UnityEngine.Object.Destroy(Output);}
    }
}
