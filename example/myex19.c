void main() {
    try{
        int i = 0;
        int n = 5;
        print i;
        print n;
        int a;
        a = n / i;
        print a;
    }
    catch("DivZero"){
        n = 0;
        print n;
    }  
}