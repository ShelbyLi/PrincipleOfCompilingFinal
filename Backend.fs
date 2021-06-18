module Backend

open System.Runtime.InteropServices

open Machine

let isLinux =
    RuntimeInformation.IsOSPlatform(OSPlatform.Linux)

let isWindows =
    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)

let isOSX =
    RuntimeInformation.IsOSPlatform(OSPlatform.OSX)

type operand = string

// 按W64/AMD调用约定计算偏移
let arg_loc (n: int) : operand =
    if isWindows then
        (match n with
         | 0 -> "rcx"
         | 1 -> "rdx"
         | 2 -> "r08"
         | 3 -> "r09"
         | _ -> $"qword [rbx + {8 * (n + 2)}]")
    else
        (match n with
         | 0 -> "rdi"
         | 1 -> "rsi"
         | 2 -> "rdx"
         | 3 -> "rcx"
         | 4 -> "r08"
         | 5 -> "r09"
         | _ -> $"qword [rbx + {8 * (n - 4)}]")

let new_label =
    let i = ref 0 in

    let get () =
        let v = !i in

        (i := (!i) + 1
         ".Lasm" + (string v))

    get

let x86header =
    "\nextern printi\n"
    + "extern printc\n"
    + "extern checkargc\n"
    + "global asm_main\n"
    + "section .data\n"
    + "glovars: dq 0\n"
    + "section .text\n"

let beforeinit argc =
    "asm_main:\n"
    + "\tpush rbp\n"
    + "\tmov qword [glovars], rsp\n"
    + "\tsub qword [glovars], 8\n"
    + $"\tpush {arg_loc 1} ;save asm_main args\n"
    + $"\tpush {arg_loc 0}\n"
    + "\t;check arg count:\n"
    + "\tsub rsp, 24\n"
    + $"\tmov {arg_loc 1}, {arg_loc 0}\n"
    + $"\tmov {arg_loc 0}, {string (argc)}\n"
    + "\tcall checkargc\n"
    + "\tadd rsp, 24\n"
    + $"\tpop {arg_loc 0}\n"
    + $"\tpop {arg_loc 1} ;pop asm_main args\n"
    + "\t; allocate globals:\n\t"

let prog = string

let rec emitx86 instr =
    match instr with
    | Label lab -> $"\n{lab}:\n\t"
    | FLabel (m, lab) ->
        $"\n{lab}:\n\t\
                    pop rax			; retaddr\n\t\
                    pop r10			; oldbp  \n\t\
                    sub rsp, 16     ; make space for svm r,bp \n\t\
                    mov rsi, rsp \n\t\
                    mov rbp, rsp \n\t\
                    add rbp, {8 * m}	   ; 8*arity \n\n\
                    _{lab}_pro_1:	  ; slide 2 stack slot\n\t\
                    cmp rbp, rsi      \n\t\
                    jz _{lab}_pro_2    \n\t\
                    mov rcx, [rsi+16] \n\t\
                    mov [rsi], rcx    \n\t\
                    add rsi, 8        \n\t\
                    jmp _{lab}_pro_1    \n\n\
                    _{lab}_pro_2: \n\t\
                    sub rbp, 8 ; rbp pointer to first arg \n\t\
                    mov [rbp+16], rax ; set retaddr \n\t\
                    mov [rbp+8], r10  ; set oldbp\n\t"

    | CSTI i ->
        $";CSTI {i}\n\t\
                    push {i}\n\t"
    | GVAR i ->
        $";GVAR {i}\n\t\
                    mov rax ,qword [glovars]\n\t\
                    sub rax , {i}*8\n\t\
                    push rax\n\t"
    | OFFSET i ->
        $";OFFSET {i}\n\t\
                    push -{i * 8}\n\t" // SVM 与x86栈的增长方向相反
    | ADD ->
        ";ADD\n\t\
                    pop rax\n\t\
                    pop r10\n\t\
                    add rax, r10\n\t\
                    push rax\n\t"
    | SUB ->
        ";SUB\n\t\
                    pop r10\n\t\
                    pop rax\n\t\
                    sub rax,r10\n\t\
                    push rax\n\t"
    | MUL ->
        ";MUL\n\t\
                    pop rax\n\t\
                    pop r10\n\t\
                    imul r10\n\t\
                    push rax\n\t"
    | DIV ->
        ";DIV\n\t\
                    pop r10\n\t\
                    pop rax\n\t\
                    cqto\n\t\
                    idiv r10\n\t\
                    push rax\n\t"

    | MOD ->
        ";MOD\n\t\
                    xor rdx,rdx\n\t\
                    pop rcx\n\t\
                    pop rax\n\t\
                    idiv rcx\n\t\
                    push rdx\n\t"
    | EQ ->
        let (l1, l2) = (new_label (), new_label ())

        $";EQ\n\t\
                    pop rax\n\t\
                    pop r10\n\t\
                    cmp rax, r10\n\t\
                    jne {l1}\n\t\
                    push 1\n\t\
                    jmp {l2}\n\
                    {l1}:\n\t\
                    push 0\n\
                    {l2}:\n\t"
    | LT ->
        let (l1, l2) = (new_label (), new_label ())

        $";LT\n\t\
                    pop rax\n\t\
                    pop r10\n\t\
                    cmp r10, rax\n\t\
                    jl {l1}\n\t\
                    push 0\n\t\
                    jmp {l2}\n\
                    {l1}:\n\t\
                    push 1\n\
                    {l2}:\n\t"
    | NOT ->
        $";NOT\n\t\
                    pop rax\n\t\
                    xor rax, 1\n\t\
                    push rax\n\t"

    | DUP ->
        $";DUP\n\t\
                    pop rax\n\t\
                    push rax\n\t\
                    push rax\n\t"
    | SWAP ->
        $";SWAP\n\t\
                    pop rax\n\t\
                    pop r10\n\t\
                    push rax\n\t\
                    push r10\n\t"
    | LDI ->
        $";LDI\n\t\
                    pop rax\n\t\
                    mov rax,[rax]\n\t\
                    push rax\n\t"

    | STI ->
        $";STI\n\t\
                    pop r10\n\t\
                    pop rax\n\t\
                    mov [rax],r10\n\t\
                    push r10\n\t"
    | GETBP ->
        $";GETBP\n\t\
                    push rbp\n\t"
    | GETSP ->
        $";GETSP\n\t\
                    push rsp\n\t"
    | INCSP m ->
        $";INCSP {m}\n\t\
                    lea rsp, [rsp-8*({m})]\n\t" // SVM 与x86栈的增长方向相反
    | GOTO lab ->
        $";GOTO {lab}\n\t\
                    jmp {lab}\n\t"
    | IFZERO lab ->
        $";IFZERO {lab}\n\t\
                    pop rax\n\t\
                    cmp rax,0\n\t\
                    je {lab}\n\t"
    | IFNZRO lab ->
        $";IFNZRO {lab}\n\t\
                    pop rax\n\t\
                    cmp rax,0\n\t\
                    jne {lab}\n\t"
    | CALL (m, lab) ->
        $";CALL {m},{lab}\n\t\
                    push rbp \n\t\
                    call near {lab}\n\t\
                    push rbx\n\t"
    | TCALL (m, n, lab) -> "tcall\n\t"
    | RET m ->
        $";RET {m}\n\t\
                    pop rbx\n\t\
                    add rsp, 8*{m}\n\t\
                    pop rbp\n\t\
                    ret\n\t"


    | PRINTI ->
        $";PRINTI\n\t\
                    pop {arg_loc 0}\n\t\
                    push {arg_loc 0}\n\t\
                    sub rsp, 16\n\t\
                    call printi\n\t\
                    add rsp, 16\n\t\
                    "
    | PRINTC ->
        $";PRINTC\n\t
                    pop {arg_loc 0}\n\t\
                    push {arg_loc 0}\n\t\
                    sub rsp, 16\n\t\
                    call printc\n\t\
                    add rsp, 16\n\t\
                    "
    | LDARGS m ->
        $"\nldargs:           ;set up command line arguments on stack:\n\t\
                    mov rcx, {arg_loc 0}\n\t\
                    mov rsi, {arg_loc 1}\n\
                    _args_next:\n\t\
                    cmp rcx, 0\n\t\
                    jz _args_end\n\t\
                    push qword [rsi]\n\t\
                    add rsi, 8\n\t\
                    sub rcx, 1\n\t\
                    jmp _args_next      ;repeat until --ecx == 0\n\
                    _args_end:\n\t\
                    lea rbp, [rsp-{m - 1}*8]  ; make rbp point to first arg\n\t"
    | STOP ->
        ";STOP\n\t\
                    mov rsp, qword [glovars]\n\t\
                    add rsp, 8          ; restore rsp\n\t\
                    pop rbp\n\t\
                    ret\n\t"
