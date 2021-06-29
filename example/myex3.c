void main(int n) { 
  int i = 0;
  int j;

  for (i=0; i<n; i=i+1) {
    print i;
    for(j=0; j<i; j=j+1) {
      print j;
    }
  }
 
}
 // i=0; 
  // for (i=0; i < n ; i=i+1) { 
  //   print i;
  //   for (j=0; j<n; j=j+1) {
  //     print j;
  //     if (j == 2) {
  //       break;
  //     }
  //   }
  //   if (i == 2) {
  //       break;
  //     }
  // } 
