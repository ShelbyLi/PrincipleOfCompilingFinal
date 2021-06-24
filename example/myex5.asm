
extern printi
extern printc
extern checkargc
global asm_main
section .data
glovars: dq 0
section .text
asm_main:
	push rbp
	mov qword [glovars], rsp
	sub qword [glovars], 8
	push rdx ;save asm_main args
	push rcx
	;check arg count:
	sub rsp, 24
	mov rdx, rcx
	mov rcx, 1
	call checkargc
	add rsp, 24
	pop rcx
	pop rdx ;pop asm_main args
	; allocate globals:
	
ldargs:           ;set up command line arguments on stack:
	mov rcx, rcx
	mov rsi, rdx
_args_next:
	cmp rcx, 0
	jz _args_end
	push qword [rsi]
	add rsi, 8
	sub rcx, 1
	jmp _args_next      ;repeat until --ecx == 0
_args_end:
	lea rbp, [rsp-0*8]  ; make rbp point to first arg
	;CALL 1,L1_main
	push rbp 
	call near L1_main
	push rbx
	;STOP
	mov rsp, qword [glovars]
	add rsp, 8          ; restore rsp
	pop rbp
	ret
	
L1_main:
	pop rax			; retaddr
	pop r10			; oldbp  
	sub rsp, 16     ; make space for svm r,bp 
	mov rsi, rsp 
	mov rbp, rsp 
	add rbp, 8	   ; 8*arity 

_L1_main_pro_1:	  ; slide 2 stack slot
	cmp rbp, rsi      
	jz _L1_main_pro_2    
	mov rcx, [rsi+16] 
	mov [rsi], rcx    
	add rsi, 8        
	jmp _L1_main_pro_1    

_L1_main_pro_2: 
	sub rbp, 8 ; rbp pointer to first arg 
	mov [rbp+16], rax ; set retaddr 
	mov [rbp+8], r10  ; set oldbp
	;INCSP 1
	lea rsp, [rsp-8*(1)]
	;GETBP
	push rbp
	;OFFSET 1
	push -8
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;CSTI 0
	push 0
	;STI
	pop r10
	pop rax
	mov [rax],r10
	push r10
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;INCSP 1
	lea rsp, [rsp-8*(1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;CSTI 0
	push 0
	;STI
	pop r10
	pop rax
	mov [rax],r10
	push r10
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;PRINTI
	pop rcx
	push rcx
	sub rsp, 16
	call printi
	add rsp, 16
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 1
	push 1
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;STI
	pop r10
	pop rax
	mov [rax],r10
	push r10
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 3
	push 3
	;EQ
	pop rax
	pop r10
	cmp rax, r10
	jne .Lasm0
	push 1
	jmp .Lasm1
.Lasm0:
	push 0
.Lasm1:
	;IFZERO L8
	pop rax
	cmp rax,0
	je L8
	;GOTO L7
	jmp L7
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	;GOTO L9
	jmp L9
	
L8:
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	
L9:
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	;GOTO L6
	jmp L6
	
L5:
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;PRINTI
	pop rcx
	push rcx
	sub rsp, 16
	call printi
	add rsp, 16
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 1
	push 1
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;STI
	pop r10
	pop rax
	mov [rax],r10
	push r10
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 3
	push 3
	;EQ
	pop rax
	pop r10
	cmp rax, r10
	jne .Lasm2
	push 1
	jmp .Lasm3
.Lasm2:
	push 0
.Lasm3:
	;IFZERO L10
	pop rax
	cmp rax,0
	je L10
	;GOTO L7
	jmp L7
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	;GOTO L11
	jmp L11
	
L10:
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	
L11:
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	
L6:
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;GETBP
	push rbp
	;OFFSET 0
	push -0
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;LT
	pop rax
	pop r10
	cmp r10, rax
	jl .Lasm4
	push 0
	jmp .Lasm5
.Lasm4:
	push 1
.Lasm5:
	;IFNZRO L5
	pop rax
	cmp rax,0
	jne L5
	
L7:
	;GETBP
	push rbp
	;OFFSET 1
	push -8
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;PRINTI
	pop rcx
	push rcx
	sub rsp, 16
	call printi
	add rsp, 16
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 1
	push -8
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;GETBP
	push rbp
	;OFFSET 1
	push -8
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 1
	push 1
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;STI
	pop r10
	pop rax
	mov [rax],r10
	push r10
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	;GOTO L3
	jmp L3
	
L2:
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;PRINTI
	pop rcx
	push rcx
	sub rsp, 16
	call printi
	add rsp, 16
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 1
	push 1
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;STI
	pop r10
	pop rax
	mov [rax],r10
	push r10
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 3
	push 3
	;EQ
	pop rax
	pop r10
	cmp rax, r10
	jne .Lasm6
	push 1
	jmp .Lasm7
.Lasm6:
	push 0
.Lasm7:
	;IFZERO L15
	pop rax
	cmp rax,0
	je L15
	;GOTO L14
	jmp L14
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	;GOTO L16
	jmp L16
	
L15:
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	
L16:
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	;GOTO L13
	jmp L13
	
L12:
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;PRINTI
	pop rcx
	push rcx
	sub rsp, 16
	call printi
	add rsp, 16
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 1
	push 1
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;STI
	pop r10
	pop rax
	mov [rax],r10
	push r10
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 3
	push 3
	;EQ
	pop rax
	pop r10
	cmp rax, r10
	jne .Lasm8
	push 1
	jmp .Lasm9
.Lasm8:
	push 0
.Lasm9:
	;IFZERO L17
	pop rax
	cmp rax,0
	je L17
	;GOTO L14
	jmp L14
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	;GOTO L18
	jmp L18
	
L17:
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	
L18:
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	
L13:
	;GETBP
	push rbp
	;OFFSET 2
	push -16
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;GETBP
	push rbp
	;OFFSET 0
	push -0
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;LT
	pop rax
	pop r10
	cmp r10, rax
	jl .Lasm10
	push 0
	jmp .Lasm11
.Lasm10:
	push 1
.Lasm11:
	;IFNZRO L12
	pop rax
	cmp rax,0
	jne L12
	
L14:
	;GETBP
	push rbp
	;OFFSET 1
	push -8
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;PRINTI
	pop rcx
	push rcx
	sub rsp, 16
	call printi
	add rsp, 16
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;GETBP
	push rbp
	;OFFSET 1
	push -8
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;GETBP
	push rbp
	;OFFSET 1
	push -8
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;CSTI 1
	push 1
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;STI
	pop r10
	pop rax
	mov [rax],r10
	push r10
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	
L3:
	;GETBP
	push rbp
	;OFFSET 1
	push -8
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;GETBP
	push rbp
	;OFFSET 0
	push -0
	;ADD
	pop rax
	pop r10
	add rax, r10
	push rax
	;LDI
	pop rax
	mov rax,[rax]
	push rax
	;LT
	pop rax
	pop r10
	cmp r10, rax
	jl .Lasm12
	push 0
	jmp .Lasm13
.Lasm12:
	push 1
.Lasm13:
	;IFNZRO L2
	pop rax
	cmp rax,0
	jne L2
	
L4:
	;INCSP -2
	lea rsp, [rsp-8*(-2)]
	;RET 0
	pop rbx
	add rsp, 8*0
	pop rbp
	ret
	