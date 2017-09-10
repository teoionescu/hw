#include <bits/stdc++.h>
using namespace std;

class Object {
private:
public:
    int data;
};

class ObjectCounter {
private:
    int cnt;
public:
    ObjectCounter(int initialCount = 0): cnt(initialCount) {}
    void incrementObjectCounter() {
        cnt++;
    }
    void decrementObjectCounter() {
        cnt--;
    }
    int getObjectCount() {
        return cnt;
    }
};

template <typename T>
class SmartReference {
private:
    const T * const obj;
    ObjectCounter * const cnt;
    SmartReference(): obj(nullptr), cnt(nullptr) {}
public:
    SmartReference(const SmartReference & other): obj(other.obj), cnt(other.cnt) {
        cnt->incrementObjectCounter();
    }
    SmartReference(const T * t) : obj(t), cnt(new ObjectCounter(1)) {}
    ~SmartReference() {
        cnt->decrementObjectCounter();
        if(cnt->getObjectCount() == 0) {
            delete obj;
        }
    }
    int getObjectCount() {
        return cnt->getObjectCount();
    }
    T& getObject () {
        return *obj;
    }
};

int main() {
    SmartReference<Object> p = new Object();
    cout<< p.getObjectCount() << '\n';
    SmartReference<Object> y = p;
    cout<< p.getObjectCount() << '\n';
    {
        SmartReference<Object> u(p);
        cout<< y.getObjectCount() << '\n';
    }
    cout<< p.getObjectCount() << '\n';
    delete &p;
    cout<< y.getObjectCount() << '\n';
    return 0;
}
