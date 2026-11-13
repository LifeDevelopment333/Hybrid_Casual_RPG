var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
var et=System.Type.GetType("UnityEditor.LogEntries,UnityEditor");
var lt=System.Type.GetType("UnityEditor.LogEntry,UnityEditor");
var property=et.GetProperty("consoleFlags",flags);var saved=(int)property.GetValue(null);
var filter=et.GetMethod("GetFilteringText",flags).Invoke(null,null);
var messages=new System.Collections.Generic.List<object>();
try{
 property.SetValue(null,(saved|(1<<7)|(1<<8)|(1<<9))&~1);
 et.GetMethod("SetFilteringText",flags).Invoke(null,new object[]{""});
 int count=(int)et.GetMethod("StartGettingEntries",flags).Invoke(null,null);
 try{
  var entry=System.Activator.CreateInstance(lt);var fields=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
  for(int i=Mathf.Max(0,count-5);i<count;i++){
   bool success=(bool)et.GetMethod("GetEntryInternal",flags).Invoke(null,new object[]{i,entry});
   messages.Add(new{success,message=lt.GetField("message",fields).GetValue(entry),mode=lt.GetField("mode",fields).GetValue(entry)});
  }
 }finally{et.GetMethod("EndGettingEntries",flags).Invoke(null,null);}
}finally{property.SetValue(null,saved);et.GetMethod("SetFilteringText",flags).Invoke(null,new object[]{filter});}
return messages;
