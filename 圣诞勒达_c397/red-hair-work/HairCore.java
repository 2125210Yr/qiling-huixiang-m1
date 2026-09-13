import com.live2d.sdk.cubism.core.*;
import java.nio.file.*;
import java.util.*;
import java.io.*;
public class HairCore {
 static String q(String s){return "\""+s.replace("\\","\\\\").replace("\"","\\\"")+"\"";}
 static String a(float[] v){return Arrays.toString(v);}
 static String a(int[] v){return Arrays.toString(v);}
 static String a(short[] v){int[] x=new int[v.length];for(int i=0;i<v.length;i++)x[i]=v[i]&65535;return a(x);}
 public static void main(String[] args)throws Exception{
  byte[] bytes=Files.readAllBytes(Path.of(args[0]));
  if(!Live2DCubismCore.hasMocConsistency(bytes))throw new Exception("MOC consistency failed");
  try(CubismMoc moc=CubismMoc.instantiate(bytes);CubismModel model=moc.instantiateModel();BufferedWriter out=Files.newBufferedWriter(Path.of(args[1]))){
   CubismParameters ps=model.getParameters();CubismDrawables ds=model.getDrawables();CubismCanvasInfo c=model.getCanvasInfo();
   System.arraycopy(ps.getDefaultValues(),0,ps.getValues(),0,ps.getCount());model.update();
   out.write("{\"type\":\"metadata\",\"canvas\":"+a(c.getSizeInPixels())+",\"origin\":"+a(c.getOriginInPixels())+",\"ppu\":"+c.getPixelsPerUnit()+",\"meshes\":[");
   for(int k=0;k<ds.getCount();k++){if(k>0)out.write(",");out.write("{\"id\":"+q(ds.getIds()[k])+",\"uv\":"+a(ds.getVertexUvs()[k])+",\"indices\":"+a(ds.getIndices()[k])+",\"masks\":"+a(ds.getMasks()[k])+",\"flags\":"+ds.getConstantFlags()[k]+",\"blendMode\":"+ds.getBlendModes()[k]+"}");}out.write("]}\n");
   List<String> frames=Files.readAllLines(Path.of(args[2]));
   for(String line:frames){String[] fields=line.split("\\t");System.arraycopy(ps.getDefaultValues(),0,ps.getValues(),0,ps.getCount());
    for(int j=1;j<fields.length;j++){String[] kv=fields[j].split("=");int i=Arrays.asList(ps.getIds()).indexOf(kv[0]);if(i>=0)ps.getValues()[i]=Float.parseFloat(kv[1]);}model.update();
    out.write("{\"type\":\"frame\",\"name\":"+q(fields[0])+",\"orders\":"+a(ds.getDrawOrders())+",\"opacities\":"+a(ds.getOpacities())+",\"positions\":[");
    for(int k=0;k<ds.getCount();k++){if(k>0)out.write(",");for(float v:ds.getVertexPositions()[k])if(!Float.isFinite(v))throw new Exception("Non-finite vertex");out.write(a(ds.getVertexPositions()[k]));}out.write("]}\n");
   }
   System.out.println("PASS official Core: "+ds.getCount()+" meshes, "+ps.getCount()+" parameters, "+frames.size()+" poses; all finite.");
  }
 }
}
