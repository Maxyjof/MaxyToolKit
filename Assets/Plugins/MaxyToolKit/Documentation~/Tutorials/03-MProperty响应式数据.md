# 03MProperty响应式数据

## 单值属性

`MProperty<T>`适合保存会驱动界面或其他逻辑的状态

```csharp
private readonly MProperty<int> coins = new MProperty<int>(0);
private IDisposable coinsSubscription;

private void OnEnable()
{
    coinsSubscription = coins.Subscribe(value => MLog.Log("金币变化：" + value), true);
}

private void OnDisable()
{
    coinsSubscription?.Dispose();
}

private void AddCoins(int value)
{
    coins.Value += value;
}
```

设置相同的值不会重复发送通知

## 列表属性

```csharp
private readonly MListProperty<string> items = new MListProperty<string>();

private void AddItem(string item)
{
    items.Add(item);
}

private void RemoveItem(string item)
{
    items.Remove(item);
}
```

回调参数是`IReadOnlyList<T>`，如果要批量修改，使用`BeginBatch`和`EndBatch`

```csharp
items.BeginBatch();
items.Add("剑");
items.Add("盾");
items.EndBatch();
```

批量操作只发送一次变化通知

## 字典属性

`MDictionaryProperty<TKey,TValue>`适合保存配置表、状态表或缓存数据

```csharp
private readonly MDictionaryProperty<string, int> levels =
    new MDictionaryProperty<string, int>();

levels["战士"] = 10;
levels["法师"] = 8;

if (levels.TryGetValue("战士", out var level))
{
    MLog.Log("战士等级：" + level);
}
```

## 使用建议

- 需要单个状态时使用`MProperty`
- 需要有顺序的数据时使用`MListProperty`
- 需要按键快速查找时使用`MDictionaryProperty`
- 不要在变化回调中直接修改同一个集合，复杂更新请使用批处理
