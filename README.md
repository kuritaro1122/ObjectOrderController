# ObjectOrderController
[Unity] オブジェクトに命令を付与する.

## Demo
```
class Cat : ObjectOrderController<Cat> {
  public void　Sound() {
    Debug.Log("meows!");
  }
}
Instantiate(prefab).AddComponent<Cat>()
  .SetAction(() => Debug.Log("start"))
  .SetYield(new WaitForSeconds(1f))
  .SetAction(c => c.Sound()) // meow!
  .SetYield(new WaitForSeconds(1f))
  .SetAction(() => Debug.Log("end"))
  .SetYield(new WaitForSeconds(1f))
  .Execute(m => Destroy(m.gameObject)); // Destroy after finish.
```
