# ObjectOrderController
[Unity] オブジェクトに命令を付与する.

## Demo
```
class Cat : ObjectOrderController<Cat> {
  public void　Sound() {
    Debug.Log("meows!");
  }
}
GameObject prefab;

Instantiate(prefab).AddComponent<Cat>()
  .SetAction(() => Debug.Log("start"))
  .SetYield(new WaitForSeconds(1f))
  .SetAction(c => c.Sound())            // meow!
  .SetYield(new WaitForSeconds(1f))
  .SetAction(() => Debug.Log("end"))
  .SetYield(new WaitForSeconds(1f))
  .Execute(m => Destroy(m.gameObject)); // Destroy after finish.
```  
```
------------------------------------
start
meow!
end
------------------------------------
```
```
int count = 0;

Instantiate(prefab).AddComponent<Cat>()
 .CreateNode(0, (false, 1, c => count % 2 == 0) (false, 0, c => true))
  .SetAction(() => Debug.Log(count))
  .SetAction(() => count++)
  .SetYield(new WaitForSeconds(1f))
 .CreateNode(1, (false, 0, () => true))
  .SetAction(c => c.Sound())
 Execute(0);
```
```
------------------------------------
0
meow!
1
2
meow!
3
4
meow!
5
------------------------------------
```
