module X86
(* assembler syntax --------------------------------------------------------- *)

#nowarn "62"

type lbl = string

type quad = int64

type imm =
    | Lit of quad
    | Lbl of lbl

(* arguments: rdi, rsi, rdx, rcx, r09, r08
   callee-save rbx, rbp, r12-r15 *)
type reg =
    | Rip
    | Rax
    | Rbx
    | Rcx
    | Rdx
    | Rsi
    | Rdi
    | Rbp
    | Rsp
    | R08
    | R09
    | R10
    | R11
    | R12
    | R13
    | R14
    | R15

type operand =
    | Imm of imm (* immediate *)
    | Reg of reg (* register *)
    | Ind1 of imm (* indirect: displacement *)
    | Ind2 of reg (* indirect: (%reg) *)
    | Ind3 of (imm * reg) (* indirect: displacement(%reg) *)

(* Condition Codes *)
type cnd =
    | Eq
    | Neq
    | Gt
    | Ge
    | Lt
    | Le

type opcode =
    | Movq
    | Pushq
    | Popq
    | Leaq
    | Incq
    | Decq
    | Negq
    | Notq
    | Addq
    | Subq
    | Imulq
    | Xorq
    | Orq
    | Andq
    | Shlq
    | Sarq
    | Shrq
    | Jmp
    | J of cnd
    | Cmpq
    | Set of cnd
    | Callq
    | Retq

type ins = opcode * (operand list)

type data =
    | Asciz of string
    | Quad of imm

type asm =
    | Text of ins list (* code *)
    | Data of data list (* data *)

(* labeled blocks of data or code *)
type elem = { lbl: lbl; globals: bool; asm: asm }

type prog = elem list


(* pretty printing ----------------------------------------------------------- *)

let string_of_reg : reg -> string =
    function
    | Rip -> "%rip"
    | Rax -> "%rax"
    | Rbx -> "%rbx"
    | Rcx -> "%rcx"
    | Rdx -> "%rdx"
    | Rsi -> "%rsi"
    | Rdi -> "%rdi"
    | Rbp -> "%rbp"
    | Rsp -> "%rsp"
    | R08 -> "%r8 "
    | R09 -> "%r9 "
    | R10 -> "%r10"
    | R11 -> "%r11"
    | R12 -> "%r12"
    | R13 -> "%r13"
    | R14 -> "%r14"
    | R15 -> "%r15"

let string_of_byte_reg : reg -> string =
    function
    | Rip -> failwith "%rip used as byte register"
    | Rax -> "%al"
    | Rbx -> "%bl"
    | Rcx -> "%cl"
    | Rdx -> "%dl"
    | Rsi -> "%sil"
    | Rdi -> "%dil"
    | Rbp -> "%bpl"
    | Rsp -> "%spl"
    | R08 -> "%r8b"
    | R09 -> "%r9b"
    | R10 -> "%r10b"
    | R11 -> "%r11b"
    | R12 -> "%r12b"
    | R13 -> "%r13b"
    | R14 -> "%r14b"
    | R15 -> "%r15b"

let string_of_lbl (l: lbl) : string = l

let string_of_imm : imm -> string =
    function
    | Lit i -> string (int64 i)
    | Lbl l -> string_of_lbl l

let string_of_operand : operand -> string =
    function
    | Imm i -> "$" ^ string_of_imm i
    | Reg r -> string_of_reg r
    | Ind1 i -> string_of_imm i
    | Ind2 r -> "(" ^ string_of_reg r ^ ")"
    | Ind3 (i, r) -> string_of_imm i ^ "(" ^ string_of_reg r ^ ")"

let string_of_byte_operand : operand -> string =
    function
    | Imm i -> "$" ^ string_of_imm i
    | Reg r -> string_of_byte_reg r
    | Ind1 i -> string_of_imm i
    | Ind2 r -> "(" ^ string_of_reg r ^ ")"
    | Ind3 (i, r) -> string_of_imm i ^ "(" ^ string_of_reg r ^ ")"

let string_of_jmp_operand : operand -> string =
    function
    | Imm i -> string_of_imm i
    | Reg r -> "*" ^ string_of_reg r
    | Ind1 i -> "*" ^ string_of_imm i
    | Ind2 r -> "*" ^ "(" ^ string_of_reg r ^ ")"
    | Ind3 (i, r) ->
        "*"
        ^ string_of_imm i ^ "(" ^ string_of_reg r ^ ")"

let string_of_cnd : cnd -> string =
    function
    | Eq -> "e"
    | Neq -> "ne"
    | Gt -> "g"
    | Ge -> "ge"
    | Lt -> "l"
    | Le -> "le"

let string_of_opcode : opcode -> string =
    function
    | Movq -> "movq"
    | Pushq -> "pushq"
    | Popq -> "popq"
    | Leaq -> "leaq"
    | Incq -> "incq"
    | Decq -> "decq"
    | Negq -> "negq"
    | Notq -> "notq"
    | Addq -> "addq"
    | Subq -> "subq"
    | Imulq -> "imulq"
    | Xorq -> "xorq"
    | Orq -> "orq"
    | Andq -> "andq"
    | Shlq -> "shlq"
    | Sarq -> "sarq"
    | Shrq -> "shrq"
    | Jmp -> "jmp"
    | J c -> "j" ^ string_of_cnd c
    | Cmpq -> "cmpq"
    | Set c -> "set" ^ string_of_cnd c
    | Callq -> "callq"
    | Retq -> "retq"

let map_concat s f l = String.concat s (List.map f l)

let string_of_shift op =
    function
    | [ Imm i; dst ] as args ->
        "  "
        ^ string_of_opcode op
          ^ "  " ^ map_concat ", " string_of_operand args
    | [ Reg Rcx; dst ] -> Printf.sprintf "  %s  %%cl, %s" (string_of_opcode op) (string_of_operand dst)
    | args ->
        failwith (
            Printf.sprintf "shift instruction has invalid operands: %s\n" (map_concat ", " string_of_operand args)
        )

let string_of_ins (op, args) : string =
    match op with
    | Shlq
    | Sarq
    | Shrq -> string_of_shift op args
    | _ ->
        let f =
            match op with
            | J _
            | Jmp
            | Callq -> string_of_jmp_operand
            | Set _ -> string_of_byte_operand
            | _ -> string_of_operand

        "  "
        ^ string_of_opcode op ^ "  " ^ map_concat ", " f args

let escaped s =
    System.Text.RegularExpressions.Regex.Escape s

let string_of_data : data -> string =
    function
    | Asciz s -> "  .asciz  " ^ "\"" ^ escaped s ^ "\""
    // | Asciz s -> "\t.asciz\t" ^ "\"" ^ (String.escaped s) ^ "\""
    | Quad i -> "  .quad  " ^ string_of_imm i

let string_of_asm : asm -> string =
    function
    | Text is -> "\t.text\n" ^ map_concat "\n" string_of_ins is
    | Data ds -> "\t.data\n" ^ map_concat "\n" string_of_data ds

let string_of_elem
    { lbl = lbl
      globals = globals
      asm = asm }
    : string =
    let sec, body =
        match asm with
        | Text is -> "\t.text\n", map_concat "\n" string_of_ins is
        | Data ds -> "\t.data\n", map_concat "\n" string_of_data ds

    let glb =
        if globals then
            "  .globl  " ^ string_of_lbl lbl ^ "\n"
        else
            "" in

    sec ^ glb ^ string_of_lbl lbl ^ ":\n" ^ body

let string_of_prog (p: prog) : string =
    // String.concat "\n" <| List.map string_of_elem p
    String.concat "\n" (List.map string_of_elem p)



(* examples ------------------------------------------------------------------ *)

(* This module defines some convenient notations that can help when
 * writing x86 assembly AST by hand. *)

// open Platform

// let prefix label =
//     if Platform.isLinux  then
//         label
//     else
//         "_" + label


module Asm =
    let (!&) i =
        Imm(Lit(int64 i)) (* int64 constants *)

    let (!&&) l = Imm(Lbl l) (* label constants *)
    let (!@) r = Reg r (* registers *)

    (* helper functions for building blocks of data or code *)
    let data l ds =
        { lbl = l
          globals = true
          asm = Data ds }

    let text l is =
        { lbl = l
          globals = false
          asm = Text is }

    let gtext l is =
        //  { lbl = prefix l; globals = true; asm = Text is }
        { lbl = l
          globals = true
          asm = Text is }



(* Example X86 assembly program (written as OCaml AST).

     Note: OS X does not allow "absolute" addressing (i.e. using
     Ind1 (Lbl "lbl_name") as a source operand of Movq).
     So this program causes an error when assembled using gcc.
*)

open Asm

let p1 : prog =
    [ text
        "foo"
        [ Xorq, [ !@Rax; !@Rax ]
          Movq, [ !&100; !@Rax ]
          Retq, [] ]
      gtext
          "_program"
          [ Xorq, [ !@Rax; !@Rax ]
            Movq, [ Ind1(Lbl "baz"); !@Rax ]
            Retq, [] ]
      data "baz" [ Quad(Lit 99L) ]
      data "quux" [ Asciz "Hello, world!" ] ]


(* This example uses "rip-relative" addressing to load the
   global value (99L) stored at label "baz". *)
let p2 : prog =
    [ text
        "foo"
        [ Xorq, [ !@Rax; !@Rax ]
          Movq, [ !&100; !@Rax ]
          Retq, [] ]
      gtext
          "_program"
          [ Xorq, [ !@Rax; !@Rax ]
            Movq, [ Ind3(Lbl "baz", Rip); !@Rax ]
            Retq, [] ]
      data "baz" [ Quad(Lit 99L) ]
      data "quux" [ Asciz "Hello, world!" ] ]

(* This x86 program computes [n] factorial via a loop.
   Note that the argument [n] is a meta-level variable.
   That means that p3 is really a "template" that, given
   an OCaml integer [n] produces assembly code to
   compute [n] factorial.
*)
let p3 (n: int) : prog =
    [ gtext
        "_program"
        [ Movq, [ !&1; !@Rax ]
          Movq, [ !&n; !@Rdi ] ]
      text
          "loop"
          [ Cmpq, [ !&0; !@Rdi ]
            J Eq, [ !&& "exit" ]
            Imulq, [ !@Rdi; !@Rax ]
            Decq, [ !@Rdi ]
            Jmp, [ !&& "loop" ] ]
      text "exit" [ Retq, [] ] ]


(* This x86 program computes [n] factorial via recursion.
   As above, [n] is a meta-level argument.
*)
let p4 (n: int) : prog =
    [ text
        "fac"
        [ Movq, [ !@Rsi; !@Rax ]
          Cmpq, [ !&1; !@Rdi ]
          J Eq, [ !&& "exit" ]
          Imulq, [ !@Rdi; !@Rsi ]
          Decq, [ !@Rdi ]
          Callq, [ !&& "fac" ] ]
      text "exit" [ Retq, [] ]
      gtext
          "_program"
          [ Movq, [ !&n; !@Rdi ]
            Movq, [ !&1; !@Rsi ]
            Callq, [ !&& "fac" ]
            Retq, [] ] ]
