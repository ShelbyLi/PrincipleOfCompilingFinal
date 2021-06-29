
int x;
int g() {
    print x;
    // return 0;
}

int f() {
    int x = 3;
    return g();
}

int main(){
     x = 10;
     f();
}