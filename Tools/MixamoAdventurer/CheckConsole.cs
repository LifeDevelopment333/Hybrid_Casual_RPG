var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
var reader=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Console.EditorConsoleEntries")).First(t=>t!=null);
var entryType=reader.GetNestedType("Entry",System.Reflection.BindingFlags.NonPublic);
var list=(System.Collections.IList)System.Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(entryType));
var success=reader.GetMethod("TryReadRecent",flags).Invoke(null,new object[]{30,list});
return new{success,entries=list.Cast<object>().Select(e=>new{type=entryType.GetField("Type").GetValue(e).ToString(),message=entryType.GetField("Message").GetValue(e)}).ToArray()};
