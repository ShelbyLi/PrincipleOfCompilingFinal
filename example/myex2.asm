
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
	;CSTI 8
	push 8
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
	;IFZERO L2
	pop rax
	cmp rax,0
	je L2
	;CSTI 1
	push 1
	;PRINTI
	pop rcx
	push rcx
	sub rsp, 16
	call printi
	add rsp, 16
	;GOTO L3
	jmp L3
	
L2:
	;CSTI 0
	push 0
	;PRINTI
	pop rcx
	push rcx
	sub rsp, 16
	call printi
	add rsp, 16
	
L3:
	;INCSP -1
	lea rsp, [rsp-8*(-1)]
	;INCSP 0
	lea rsp, [rsp-8*(0)]
	;RET 0
	pop rbx
	add rsp, 8*0
	pop rbp
	ret
	