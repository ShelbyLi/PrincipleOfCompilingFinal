int main(){
    int i;
    int a[10];
    char c[5];
    c[0] = 'h';
    c[1] = 'e';
    c[2] = 'l';
    c[3] = 'l';
    c[4] = 'o';
    for(i = 0; i < 10; ++i){
        a[i] = i;
    }
    for(i = 0; i < 10; ++i){
        print a[i];
    }
    println;
    for(i = 0; i < 5; ++i){
        print c[i];
    }
}