/* File MicroC/machine.c
   A unified-stack abstract machine for imperative programs.
   sestoft@itu.dk * 2009-10-18

   Compile like this, on ssh.itu.dk say:

      gcc -O3 -Wall machine.c -o machine

   If necessary, force compiler to use 32 bit integers:
      gcc -O3 -m32 -Wall machine.c -o machine

   To execute a program file using this abstract machine, do:
      machine <programfile> <arg1> <arg2> ...
   To get also a trace of the program execution:
      machine -trace <programfile> <arg1> <arg2> ...
*/

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <sys/time.h>
#ifndef _WIN32
#include <sys/resource.h>
#endif
// These numeric instruction codes must agree with MicroC/Machine.fs:
// (Use #define because const int does not define a constant in C)

#define CSTI 0
#define ADD 1
#define SUB 2
#define MUL 3
#define DIV 4
#define MOD 5
#define EQ 6
#define LT 7
#define NOT 8
#define DUP 9
#define SWAP 10
#define LDI 11
#define STI 12
#define GETBP 13
#define GETSP 14
#define INCSP 15
#define GOTO 16
#define IFZERO 17
#define IFNZRO 18
#define CALL 19
#define TCALL 20
#define RET 21
#define PRINTI 22
#define PRINTC 23
#define LDARGS 24
#define STOP 25
#define BITAND 26
#define BITOR 27
#define BITXOR 28
#define BITLEFT 29
#define BITRIGHT 30
#define BITNOT 31
#define THROW 32
#define PUSHHR 33
#define POPHR 34
#define NEG 35
#define PRINTF 36
#define STACKSIZE 1000


typedef union 
{
	struct 
	{
		unsigned char low_byte;
        unsigned char mlow_byte;
        unsigned char mhigh_byte;
        unsigned char high_byte;
     }float_byte;
          
     float  value;
}FLAOT_UNION;

// Print the stack machine instruction at p[pc]

void printInstruction(int p[], int pc)
{
  switch (p[pc])
  {
  case CSTI:
    printf("CSTI %d", p[pc + 1]);
    break;
  case ADD:
    printf("ADD");
    break;
  case SUB:
    printf("SUB");
    break;
  case MUL:
    printf("MUL");
    break;
  case DIV:
    printf("DIV");
    break;
  case MOD:
    printf("MOD");
    break;
  case EQ:
    printf("EQ");
    break;
  case LT:
    printf("LT");
    break;
  case NOT:
    printf("NOT");
    break;
  case DUP:
    printf("DUP");
    break;
  case SWAP:
    printf("SWAP");
    break;
  case LDI:
    printf("LDI");
    break;
  case STI:
    printf("STI");
    break;
  case GETBP:
    printf("GETBP");
    break;
  case GETSP:
    printf("GETSP");
    break;
  case INCSP:
    printf("INCSP %d", p[pc + 1]);
    break;
  case GOTO:
    printf("GOTO %d", p[pc + 1]);
    break;
  case IFZERO:
    printf("IFZERO %d", p[pc + 1]);
    break;
  case IFNZRO:
    printf("IFNZRO %d", p[pc + 1]);
    break;
  case CALL:
    printf("CALL %d %d", p[pc + 1], p[pc + 2]);
    break;
  case TCALL:
    printf("TCALL %d %d %d", p[pc + 1], p[pc + 2], p[pc + 3]);
    break;
  case RET:
    printf("RET %d", p[pc + 1]);
    break;
  case PRINTI:
    printf("PRINTI");
    break;
  case PRINTC:
    printf("PRINTC");
    break;
  case LDARGS:
    printf("LDARGS");
    break;
  case STOP:
    printf("STOP");
    break;
  case BITLEFT: 
        printf("BITLEFT");
        break;
  case BITRIGHT: 
        printf("BITRIGHT");
        break;
  case BITAND: 
        printf("BITAND");
        break;
  case BITOR: 
        printf("BITOR");
        break;
  case BITXOR: 
        printf("BITXOR");
        break;
  case BITNOT: 
        printf("BITNOT");
        break;
  case THROW:
        printf("THROW %d", p[pc + 1]);
        break;
  case PUSHHR: 
        printf("PUSHHR %d %d", p[pc + 1], p[pc + 2]);
        break;
  case POPHR: 
        printf("POPHR");
        break;
  case NEG:
        printf("NEG");
        break;
  case PRINTF:
    printf("PRINTF");
  default:
    printf("<unknown>");
    break;
  }
}

// Print current stack and current instruction

void printStackAndPc(int s[], int bp, int sp, int p[], int pc)
{
  printf("[ ");
  int i;
  for (i = 0; i <= sp; i++)
    printf("%d ", s[i]);
  printf("]");
  printf("{%d:", pc);
  printInstruction(p, pc);
  printf("}\n");
}

// Read instructions from a file, return array of instructions

int *readfile(char *filename)
{
  int capacity = 1, size = 0;
  int *program = (int *)malloc(sizeof(int) * capacity);
  FILE *inp = fopen(filename, "r");
  int instr;
  while (fscanf(inp, "%d", &instr) == 1)
  {
    if (size >= capacity)
    {
      int *buffer = (int *)malloc(sizeof(int) * 2 * capacity);
      int i;
      for (i = 0; i < capacity; i++)
        buffer[i] = program[i];
      free(program);
      program = buffer;
      capacity *= 2;
    }
    program[size++] = instr;
  }
  fclose(inp);
  return program;
}

// The machine: execute the code starting at p[pc]

int execcode(int p[], int s[], int iargs[], int iargc, int /* boolean */ trace)
{
  int bp = -999; // Base pointer, for local variable access
  int sp = -1;   // Stack top pointer
  int pc = 0;    // Program counter: next instruction
  int hr = -1;

  char temp[4];
  unsigned short m=0;
  float CurrentReceiveData=0;
  void   *pf;
  pf = &CurrentReceiveData;
  unsigned char * px;

  for (;;)
  {
    if (trace)
      printStackAndPc(s, bp, sp, p, pc);
    switch (p[pc++])
    {
    case CSTI:
      s[sp + 1] = p[pc++];
      sp++;
      break;
    case ADD:
      s[sp - 1] = s[sp - 1] + s[sp];
      sp--;
      break;
    case SUB:
      s[sp - 1] = s[sp - 1] - s[sp];
      sp--;
      break;
    case MUL:
      s[sp - 1] = s[sp - 1] * s[sp];
      sp--;
      break;
    case DIV:
      // s[sp - 1] = s[sp - 1] / s[sp];
      // sp--;

      if(s[sp]==0)
      {
        // printf("hr: %d ", hr);
        printf("exception: %d %s\n", 1, "Div error");
          while (hr != -1 && s[hr] != 1 )
          {
              hr = s[hr+2];
            // printf("hr: %d ", hr);
              printf("exception: %d\n", p[pc]);
          }
              
          if (hr != -1) { 
              sp = hr-1;    
              pc = s[hr+1];
              hr = s[hr+2];
          } else {
              printf("%d not found exception\n", hr);
              return sp;
          }
      }
      else{
          s[sp - 1] = s[sp-1] / s[sp];
          sp--; 
      }
                    

      break;
    case MOD:
      s[sp - 1] = s[sp - 1] % s[sp];
      sp--;
      break;
    case EQ:
      s[sp - 1] = (s[sp - 1] == s[sp] ? 1 : 0);
      sp--;
      break;
    case LT:
      s[sp - 1] = (s[sp - 1] < s[sp] ? 1 : 0);
      sp--;
      break;
    case NOT:
      s[sp] = (s[sp] == 0 ? 1 : 0);
      break;
    case DUP:
      s[sp + 1] = s[sp];
      sp++;
      break;
    case SWAP:
    {
      int tmp = s[sp];
      s[sp] = s[sp - 1];
      s[sp - 1] = tmp;
    }
    break;
    case LDI: // load indirect
      s[sp] = s[s[sp]];
      break;
    case STI: // store indirect, keep value on top
      s[s[sp - 1]] = s[sp];
      s[sp - 1] = s[sp];
      sp--;
      break;
    case GETBP:
      s[sp + 1] = bp;
      sp++;
      break;
    case GETSP:
      s[sp + 1] = sp;
      sp++;
      break;
    case INCSP:
      sp = sp + p[pc++];
      break;
    case GOTO:
      pc = p[pc];
      break;
    case IFZERO:
      pc = (s[sp--] == 0 ? p[pc] : pc + 1);
      break;
    case IFNZRO:
      pc = (s[sp--] != 0 ? p[pc] : pc + 1);
      break;
    case CALL:
    {
      int argc = p[pc++];
      int i;
      for (i = 0; i < argc; i++)   // Make room for return address
        s[sp - i + 2] = s[sp - i]; // and old base pointer
      s[sp - argc + 1] = pc + 1;
      sp++;
      s[sp - argc + 1] = bp;
      sp++;
      bp = sp + 1 - argc;
      pc = p[pc];
    }
    break;
    case TCALL:
    {
      int argc = p[pc++]; // Number of new arguments
      int pop = p[pc++];  // Number of variables to discard
      int i;
      for (i = argc - 1; i >= 0; i--) // Discard variables
        s[sp - i - pop] = s[sp - i];
      sp = sp - pop;
      pc = p[pc];
    }
    break;
    case RET:
    {
      int res = s[sp];
      sp = sp - p[pc];
      bp = s[--sp];
      pc = s[--sp];
      s[sp] = res;
    }
    break;
    case PRINTI:
      printf("%d ", s[sp]);
      break;
    case PRINTC:
      printf("%c", s[sp]);
      break;
    case LDARGS:
    {
      int i;
      for (i = 0; i < iargc; i++) // Push commandline arguments
        s[++sp] = iargs[i];
    }
    break;
    case STOP:
      return 0;
    case BITLEFT: 
          s[sp-1] = s[sp-1] << s[sp]; sp--; break;
    case BITRIGHT: 
        s[sp-1] = s[sp-1] >> s[sp]; sp--; break;
    case BITAND: 
        s[sp-1] = s[sp-1] & s[sp]; sp--; break;
    case BITOR: 
        s[sp-1] = s[sp-1] | s[sp]; sp--; break;
    case BITXOR: 
          s[sp-1] = s[sp-1] ^ s[sp]; sp--; break;
	  case BITNOT: 
          s[sp] = ~s[sp]; break;
    case PUSHHR:
      s[++sp] = p[pc++];    //exn
      int tmp = sp;       //exn address
      sp++;
      s[sp++] = p[pc++];   //jump address
      s[sp] = hr;
      hr = tmp;
      break;
    case POPHR:
        hr = s[sp--];
        sp-=2;
        break;
	  case THROW: 
      while (hr != -1 && s[hr] != p[pc] )
      {
          hr = s[hr+2]; //find exn address
          // System.out.println("hr:"+hr+" exception:"+new WIntType(program.get(pc)).getValue());
          printf("hr: %d exception: %d\n", hr, p[pc]);
      }
          
      if (hr != -1) { // Found a handler for exn
          sp = hr-1;    // remove stack after hr
          pc = s[hr+1];
          hr = s[hr+2]; // with current handler being hr
      } else {
          // System.out.print(hr+"not find exception");
          printf("%d not find exception\n");
          return sp;
      }
      break;
	  case NEG:
          s[sp] = -s[sp];
          break;
	  case PRINTF:
          // FLAOT_UNION funion;
          // for(m=0; m<4; m++)//int转byte
          // {
          //     temp[m+1]=(s[sp]>>(24-m*8));
          // }
          // printf("%d ", temp[0]);
          // funion.low_byte = temp[0];
          // printf("%d ", temp[1]);
          // funion.mlow_byte = temp[1];
          // printf("%d ", temp[2]);
          // funion.mhigh_byte = temp[2];
          // printf("%d\n", temp[3]);
          // funion.high_byte = temp[3];
          // printf("%d\n", s[sp]);

          // // byte 2 float
          // px = (unsigned char *)temp;
          // for(m=0;m<4;m++)
          // {
          //   *((unsigned char *)pf+m)=*(px+m+1);
          // }

          // printf("%f ", &funion.float);
          // // printf("%f ", (float)s[sp]);
          printf("%f", s[sp]);
          break;
    default:
      printf("Illegal instruction %d at address %d\n", p[pc - 1], pc - 1);
      return -1;
    }
  }
}

// Read program from file, and execute it

int execute(int argc, char **argv, int /* boolean */ trace)
{
  int *p = readfile(argv[trace ? 2 : 1]);          // program bytecodes: int[]
  int *s = (int *)malloc(sizeof(int) * STACKSIZE); // stack: int[]
  int iargc = trace ? argc - 3 : argc - 2;
  int *iargs = (int *)malloc(sizeof(int) * iargc); // program inputs: int[]
  int i;
  for (i = 0; i < iargc; i++) // Convert commandline arguments
    iargs[i] = atoi(argv[trace ? i + 3 : i + 2]);
// Measure cpu time for executing the program
#ifndef _WIN32
  struct rusage ru1, ru2;
  getrusage(RUSAGE_SELF, &ru1);
#endif

  int res = execcode(p, s, iargs, iargc, trace); // Execute program proper

#ifndef _WIN32
  getrusage(RUSAGE_SELF, &ru2);
  struct timeval t1 = ru1.ru_utime, t2 = ru2.ru_utime;
  double runtime = t2.tv_sec - t1.tv_sec + (t2.tv_usec - t1.tv_usec) / 1000000.0;
  printf("Used %7.3f cpu seconds\n", runtime);
#endif

  return res;
}

// Read code from file and execute it

int main(int argc, char **argv)
{
  if (argc < 2)
  {
    printf("Usage: machine [-trace] <programfile *.out> <arg1> ...\n");
    return -1;
  }
  else
  {
    int trace = argc >= 3 && 0 == strncmp(argv[1], "-trace", 7);
    return execute(argc, argv, trace);
  }
}
