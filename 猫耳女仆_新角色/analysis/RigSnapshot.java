import com.live2d.sdk.cubism.core.*;
import java.nio.file.*;
import java.util.*;
import java.io.*;

/** Read-only Core sampling. Manifest rows: moc path, output path, poses TSV path. */
public class RigSnapshot {
 static String q(String s){return "\""+s.replace("\\","\\\\").replace("\"","\\\"")+"\"";}
 static String a(float[] v){return Arrays.toString(v);}
 static String a(int[] v){return Arrays.toString(v);}
 static String a(short[] v){int[] x=new int[v.length];for(int i=0;i<v.length;i++)x[i]=v[i]&65535;return a(x);}
 public static void main(String[] args)throws Exception{
  int ok=0,failed=0;
  for(String entry:Files.readAllLines(Path.of(args[0]))){
   String[] row=entry.split("\t");
   try{
    byte[] bytes=Files.readAllBytes(Path.of(row[0]));
    if(!Live2DCubismCore.hasMocConsistency(bytes))throw new Exception("MOC consistency failed");
    try(CubismMoc moc=CubismMoc.instantiate(bytes);CubismModel model=moc.instantiateModel();BufferedWriter out=Files.newBufferedWriter(Path.of(row[1]))){
     CubismParameters ps=model.getParameters();CubismDrawables ds=model.getDrawables();CubismCanvasInfo c=model.getCanvasInfo();
     CubismParts parts=model.getParts();float[] partDefaults=parts.getOpacities().clone();
     System.arraycopy(ps.getDefaultValues(),0,ps.getValues(),0,ps.getCount());model.update();
     out.write("{\"type\":\"metadata\",\"canvas\":"+a(c.getSizeInPixels())+",\"origin\":"+a(c.getOriginInPixels())+",\"ppu\":"+c.getPixelsPerUnit()+",\"parameters\":[");
     for(int k=0;k<ps.getCount();k++){if(k>0)out.write(",");out.write("{\"id\":"+q(ps.getIds()[k])+",\"min\":"+ps.getMinimumValues()[k]+",\"max\":"+ps.getMaximumValues()[k]+",\"default\":"+ps.getDefaultValues()[k]+"}");}
     out.write("],\"meshes\":[");
     for(int k=0;k<ds.getCount();k++){if(k>0)out.write(",");out.write("{\"id\":"+q(ds.getIds()[k])+",\"uv\":"+a(ds.getVertexUvs()[k])+",\"indices\":"+a(ds.getIndices()[k])+",\"masks\":"+a(ds.getMasks()[k])+",\"flags\":"+ds.getConstantFlags()[k]+",\"blendMode\":"+ds.getBlendModes()[k]+",\"texture\":"+ds.getTextureIndices()[k]+"}");}out.write("]}\n");
     for(String line:Files.readAllLines(Path.of(row[2]))){
      String[] fields=line.split("\t");System.arraycopy(ps.getDefaultValues(),0,ps.getValues(),0,ps.getCount());System.arraycopy(partDefaults,0,parts.getOpacities(),0,parts.getCount());
      for(int j=1;j<fields.length;j++){
       String[] kv=fields[j].split("=");
       if(kv[0].startsWith("part:")){int i=Arrays.asList(parts.getIds()).indexOf(kv[0].substring(5));if(i<0)throw new Exception("Unknown part: "+kv[0]);parts.getOpacities()[i]=Float.parseFloat(kv[1]);}
       else {int i=Arrays.asList(ps.getIds()).indexOf(kv[0]);if(i<0)throw new Exception("Unknown parameter: "+kv[0]);ps.getValues()[i]=Float.parseFloat(kv[1]);}
      }model.update();
      out.write("{\"type\":\"frame\",\"name\":"+q(fields[0])+",\"orders\":"+a(ds.getDrawOrders())+",\"opacities\":"+a(ds.getOpacities())+",\"multiply\":[");
      for(int k=0;k<ds.getCount();k++){if(k>0)out.write(",");out.write(a(ds.getMultiplyColors()[k]));}out.write("],\"screen\":[");
      for(int k=0;k<ds.getCount();k++){if(k>0)out.write(",");out.write(a(ds.getScreenColors()[k]));}out.write("],\"positions\":[");
      for(int k=0;k<ds.getCount();k++){if(k>0)out.write(",");for(float v:ds.getVertexPositions()[k])if(!Float.isFinite(v))throw new Exception("Non-finite vertex");out.write(a(ds.getVertexPositions()[k]));}out.write("]}\n");
     }
    }
    ok++;
   }catch(Exception e){failed++;System.out.println("FAIL "+row[0]+": "+e);}
  }
  System.out.println("Core sampling: "+ok+" passed, "+failed+" failed.");
  if(failed>0)System.exit(1);
 }
}
