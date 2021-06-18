(* Lexing and parsing of micro-C programs using fslex and fsyacc *)

module Parse

open System
open System.IO
open System.Text
open FSharp.Text
open Absyn
open Debug

(* Plain parsing from a string, with poor error reporting *)

let fromString (str : string) : program =
    let lexbuf = Lexing.LexBuffer<char>.FromString(str)
    try 
      CPar.Main CLex.Token lexbuf
    with 
      | exn -> let pos = lexbuf.EndPos 
               failwithf "%s near line %d, column %d\n" 
                  (exn.Message) (pos.Line+1) pos.Column

// 词法分析程序，info 在调试的时候被调用，显示Token
// CLex.Token 词法分析程序入口
let token buf = 
    let res = CLex.Token buf
    msg <|
          match res with
           |CPar.EOF -> sprintf "%A\n" res
           |_ -> sprintf "%A, " res
           
    res
(* Parsing from a file *)
let fromFile (filename : string) =
    use reader = new StreamReader(filename)
    let lexbuf = Lexing.LexBuffer<char>.FromTextReader reader
    try 
      msg "\nToken:\n"
      
      //CPar.Main  语法分析主程序 
      let ast = CPar.Main token lexbuf in
        msg "\nAST:\n";
        ast
    with 
      | exn -> let pos = lexbuf.EndPos 
               failwithf "%s in file %s near line %d, column %d\n" 
                  (exn.Message) filename (pos.Line+1) pos.Column