// micro-C example 1
int g ;
int h[3] ;
void main(int n) {
  h[0] = 1;
  // h[4] = 5;
  // print h[3]; //数组首地址
  // print h[4]; //参数 n
  // h[5] = 5;
  // h[6] = 5;
  // h[7] = 5;
  // h[9] = 5;
  // h[10] = 5;
  // h[11] = 5;
  // h[12] = 5;
  // 数组越界，程序的行为会异常
  // 此时解释器，栈式虚拟机，x86程序表现各不相同，可以思考原因

  while (n > 0) {
    print n;
    n = n - 1;
  }
  println;
}
