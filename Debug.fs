module Debug

let argv = System.Environment.GetCommandLineArgs()
let mutable debug = false

try
    let _ = Array.find ((=) "-g") argv
    debug <- true
// with :? System.Exception as ex -> ()
with ex -> ()

let msg info = 
  if debug then printf "%s" info else ()


let list2str list = 
  String.concat "" (List.map string list)

let map2str store = 
  Map.toList store |> list2str
