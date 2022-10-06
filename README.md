# ObjectOrderController

MonoBehaviourを継承したクラスに命令を付与し, 非同期的に逐次実行する.
このクラスを継承することで, 直接メンバ関数の呼び出しを予約することができ, ステートを管理しながら命令を逐次実行する.\
[MovementManager](https://github.com/kuritaro1122/MovementManager/) や [RotateManager](https://github.com/kuritaro1122/RotateManager) と組み合わせることで, 敵やボスを作ることが可能.

※[FunctionExecutor](https://github.com/kuritaro1122/FunctionExecutor/), [EntityActionCon](https://github.com/kuritaro1122/EntityActionCon) の完全上位互換です.

# Requirement

* UnityEngine
* System
* System.Collections
* System.Collections.Generic

# Usage
① 任意のクラスにObjectOrderController<>を継承\
② Set関数で命令を付与\
③ Execute関数で実行
```cs
class Hoge : ObjectOrderController<Hoge> {
    void Start() {
        float seconds = 1f;
        bool flag1 = false;
        
        // 命令を付与
        base.SetAction(m => Debug.Log($"{m} start"))
        .SetYield(new WaitForSeconds(seconds))
        .SetAction(() => Debug.Log($"seconds {seconds}"))
        .SetYield(new WaitUntil(flag1))
        .SetAction(m => Debug.Log($"{m} end"))

        // 命令を実行
        .Execute();
    }
}
```
```cs
class Hoge2 : ObjectOrderController<Hoge2> {
    const bool Interrupt = true;
    const bool NoInterrupt = false;
    enum State {
        Sleep = 0,
        Awake = 1,
        Stay = 2,
        Attack = 3,
    }
    static bool SleepCondition(Hoge2 self) {
        // return sleep condition
    }
    static bool AwakeCondition(Hoge2 self) {
        // return awake condition
    }
    static bool AttackCondition(Hoge2 self) {
        // return attack condition
    }

    void Start() {
        // Sleep
        base.CreateNode((int)State.Sleep, loop: true, (Interrupt, (int)State.Awake, AwakeCondition))
        .SetAction(() => Debug.Log("zz.."))
        .SetYield(new WaitForSeconds(2f));
        // Awake
        base.CreateNode((int)State.Awake, nextIndex: (int)State.Stay)
        .SetAction(() => Debug.Log("Awake"))
        // Stay
        base.CreateNode((int)State.Stay, loop: true, (Interrupt, (int)State.Attack, AttackCondition), (Interrupt, (int)State.Sleep, SleepCondition))
        .SetAction(() => Debug.Log("buzz!!"))
        .SetYield(new WaitForSeconds(1f));
        // Attack
        base.CreateNode((int)State.Attack, (NoInterrupt, (int)State.Stay, s => !AttackCondition(s)))
        .SetCoroutine(AttackCoroutine)

        base.Execute((int)State.Sleep); // First select node index.
    }

    static void AttackCoroutine(Hoge2 self) {
        Debug.Log("Attack1");
        yield return new WaitForSeconds(0.5f);
        Debug.Log("Attack2");
        yield return new WaitForSeconds(3f);
        Debug.Log("Attack3");
    }
}
```

## Public Variable
```cs
T Self { get; }
int NodeIndex { get; }
int CurrentNodeLoopedCount { get; }
bool Running { get; }
```
## Public Function
```cs
// Node
ObjectOrderController<T> CreateNode(int index, bool loop = false, params (bool, int, Func<T, bool>)[] nodeTransitions)
ObjectOrderController<T> CreateNode(int index, int nextIndex, params (bool, int, Func<T, bool>)[] nodeTransitions)
ObjectOrderController<T> CreateNode(int index, params (bool, int ,Func<T, bool>)[] nodeTransitions)
// Set order
ObjectOrderController<T> Set(params IOrder<T>[] orders)
ObjectOrderController<T> SetYield(YieldInstruction yieldInstruction)
ObjectOrderController<T> SetYield(CustomYieldInstruction yieldInstruction)
ObjectOrderController<T> SetYield(Func<T, YieldInstruction> yieldInstruction)
ObjectOrderController<T> SetYield(Func<T, CustomYieldInstruction> yieldInstruction)
ObjectOrderController<T> SetAction(Action<T> action)
ObjectOrderController<T> SetAction(Action action)
ObjectOrderController<T> SetCoroutine(Func<T, IEnumerator> enumerator, bool isNotSync = false)
ObjectOrderController<T> SetCoroutine(Func<IEnumerator> enumerator, bool isNotSync = false)
Coroutine ExtraOrder(IOrder<T> order)
Coroutine ExtraOrder(Func<IEnumerator<T>> enumerator)
Coroutine ExtraOrder(Func<IEnumerator> enumerator)
// Control
void Execute(int index = 0)
ObjectOrderController<T> StopAll()
// void Execute(int index = 0, Action<T> finishCallBack = null)
// void Execute(int index = 0, Action finishCallBack = null)
// void Execute(Action<T> finishCallBack = null)
// void Execute(Action finishCallBack = null)
```

# Note
* StartCoroutineの代わりにExtraOrderを使用してください. ExtraOrderを使用すると内部的にStartCoroutineが呼ばれ, Nodeによって監視されます. Nodeが変更された際には他の命令と同様に中断されます.
* SetCoroutineのisNotSyncをtrueにすると後ろの命令を待たせません.

# License

"ObjectOrderController" is under [MIT license](https://en.wikipedia.org/wiki/MIT_License).
