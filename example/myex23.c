// int x;
int g(int z) {
    return z+1;
}
int f(int y) {
    int x = y + 1;
    return g(y*x);
    // return y;
}

void main() {

    print f(3);

}