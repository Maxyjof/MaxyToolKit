using System;
using System.Collections;
using System.Collections.Generic;

namespace MaxyToolKit.Data
{
    /// <summary>
    /// 保存单个值并在值变化时通知订阅者
    /// </summary>
    /// <typeparam name="T">
    /// 属性值类型
    /// </typeparam>
    public sealed class MProperty<T>
    {
        private T _value;
        private int _batchDepth;
        private bool _dirty;
        private event Action<T> _changed;

        /// <summary>
        /// 创建一个值属性
        /// </summary>
        /// <param name="initialValue">
        /// 初始值
        /// </param>
        public MProperty(T initialValue = default) => _value = initialValue;

        /// <summary>
        /// 获取或设置当前值
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value)) return;
                _value = value;
                Notify();
            }
        }

        /// <summary>
        /// 订阅值变化事件
        /// </summary>
        /// <param name="listener">
        /// 值变化时调用的回调
        /// </param>
        /// <param name="invokeImmediately">
        /// 是否先使用当前值调用一次回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable Subscribe(Action<T> listener, bool invokeImmediately = false)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (invokeImmediately) listener(_value);
            _changed += listener;
            return new MDisposable(() => _changed -= listener);
        }

        /// <summary>
        /// 订阅值变化事件
        /// </summary>
        /// <param name="listener">
        /// 值变化时调用的回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable Register(Action<T> listener) => Subscribe(listener);

        /// <summary>
        /// 订阅值变化事件并立即收到当前值
        /// </summary>
        /// <param name="listener">
        /// 值变化时调用的回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable RegisterWithInitValue(Action<T> listener) => Subscribe(listener, true);

        /// <summary>
        /// 静默替换当前值而不触发通知
        /// </summary>
        /// <param name="next">
        /// 新的值
        /// </param>
        public void SetValueSilently(T next) => _value = next;

        /// <summary>
        /// 开始批量更新并合并后续通知
        /// </summary>
        public void BeginBatch() => _batchDepth++;

        /// <summary>
        /// 结束批量更新并按需发送一次通知
        /// </summary>
        /// <param name="notify">
        /// 是否发送合并后的通知
        /// </param>
        public void EndBatch(bool notify = true)
        {
            //嵌套批处理只在最外层结束时结算
            if (_batchDepth == 0) return;
            _batchDepth--;
            if (_batchDepth != 0) return;
            //根据脏状态决定是否发送合并通知
            var shouldNotify = _dirty && notify;
            _dirty = false;
            if (shouldNotify) _changed?.Invoke(_value);
        }

        /// <summary>
        /// 清除全部值变化订阅
        /// </summary>
        public void Clear() => _changed = null;

        /// <summary>
        /// 将当前值转换为字符串
        /// </summary>
        /// <returns>
        /// 当前值的字符串表示
        /// </returns>
        public override string ToString() => _value?.ToString();

        /// <summary>
        /// 根据批处理状态通知值变化订阅者
        /// </summary>
        private void Notify()
        {
            //批量更新期间只记录脏状态
            if (_batchDepth > 0) { _dirty = true; return; }
            _changed?.Invoke(_value);
        }

        /// <summary>
        /// 将MProperty隐式转换为内部值
        /// </summary>
        /// <param name="property">
        /// 要转换的属性
        /// </param>
        /// <returns>
        /// 属性当前值
        /// </returns>
        public static implicit operator T(MProperty<T> property) => property.Value;
    }

    /// <summary>
    /// 提供带变化通知的列表属性
    /// </summary>
    /// <typeparam name="T">
    /// 列表元素类型
    /// </typeparam>
    public sealed class MListProperty<T> : IEnumerable<T>
    {
        private readonly List<T> _values;
        private int _batchDepth;
        private bool _dirty;
        private event Action<IReadOnlyList<T>> _changed;

        /// <summary>
        /// 创建空列表属性
        /// </summary>
        public MListProperty() => _values = new List<T>();

        /// <summary>
        /// 使用初始集合创建列表属性
        /// </summary>
        /// <param name="initial">
        /// 初始元素集合
        /// </param>
        public MListProperty(IEnumerable<T> initial) => _values = new List<T>(initial);

        /// <summary>
        /// 获取当前元素数量
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// 访问指定索引的元素
        /// </summary>
        /// <param name="index">
        /// 元素索引
        /// </param>
        /// <returns>
        /// 指定位置的元素
        /// </returns>
        public T this[int index]
        {
            get => _values[index];
            set { _values[index] = value; Notify(); }
        }

        /// <summary>
        /// 订阅列表变化事件
        /// </summary>
        /// <param name="listener">
        /// 列表变化时调用的回调
        /// </param>
        /// <param name="invokeImmediately">
        /// 是否先使用当前列表调用一次回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable Subscribe(Action<IReadOnlyList<T>> listener, bool invokeImmediately = false)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (invokeImmediately) listener(_values);
            _changed += listener;
            return new MDisposable(() => _changed -= listener);
        }

        /// <summary>
        /// 订阅列表变化事件
        /// </summary>
        /// <param name="listener">
        /// 列表变化时调用的回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable Register(Action<IReadOnlyList<T>> listener) => Subscribe(listener);

        /// <summary>
        /// 订阅列表变化事件并立即收到当前列表
        /// </summary>
        /// <param name="listener">
        /// 列表变化时调用的回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable RegisterWithInitValue(Action<IReadOnlyList<T>> listener) => Subscribe(listener, true);

        /// <summary>
        /// 开始批量更新并合并后续通知
        /// </summary>
        public void BeginBatch() => _batchDepth++;

        /// <summary>
        /// 结束批量更新并按需发送一次通知
        /// </summary>
        /// <param name="notify">
        /// 是否发送合并后的通知
        /// </param>
        public void EndBatch(bool notify = true)
        {
            //嵌套批处理只在最外层结束时结算
            if (_batchDepth == 0) return;
            _batchDepth--;
            if (_batchDepth != 0) return;
            //根据脏状态决定是否发送合并通知
            var shouldNotify = _dirty && notify;
            _dirty = false;
            if (shouldNotify) _changed?.Invoke(_values);
        }

        /// <summary>
        /// 添加一个元素
        /// </summary>
        /// <param name="item">
        /// 要添加的元素
        /// </param>
        public void Add(T item) { _values.Add(item); Notify(); }

        /// <summary>
        /// 静默替换全部元素
        /// </summary>
        /// <param name="items">
        /// 新的元素集合
        /// </param>
        public void ReplaceSilently(IEnumerable<T> items)
        {
            //替换全部元素并保持静默状态
            _values.Clear();
            _values.AddRange(items);
            _dirty = false;
        }

        /// <summary>
        /// 添加多个元素
        /// </summary>
        /// <param name="items">
        /// 要添加的元素集合
        /// </param>
        public void AddRange(IEnumerable<T> items) { var before = _values.Count; _values.AddRange(items); if (_values.Count != before) Notify(); }

        /// <summary>
        /// 在指定位置插入元素
        /// </summary>
        /// <param name="index">
        /// 插入位置
        /// </param>
        /// <param name="item">
        /// 要插入的元素
        /// </param>
        public void Insert(int index, T item) { _values.Insert(index, item); Notify(); }

        /// <summary>
        /// 移除第一个匹配元素
        /// </summary>
        /// <param name="item">
        /// 要移除的元素
        /// </param>
        /// <returns>
        /// 成功移除时返回true
        /// </returns>
        public bool Remove(T item) { var result = _values.Remove(item); if (result) Notify(); return result; }

        /// <summary>
        /// 移除所有匹配条件的元素
        /// </summary>
        /// <param name="predicate">
        /// 筛选条件
        /// </param>
        /// <returns>
        /// 移除的元素数量
        /// </returns>
        public int RemoveAll(Predicate<T> predicate) { var result = _values.RemoveAll(predicate); if (result > 0) Notify(); return result; }

        /// <summary>
        /// 移除指定位置的元素
        /// </summary>
        /// <param name="index">
        /// 元素索引
        /// </param>
        public void RemoveAt(int index) { _values.RemoveAt(index); Notify(); }

        /// <summary>
        /// 清空全部元素
        /// </summary>
        public void Clear() { if (_values.Count == 0) return; _values.Clear(); Notify(); }

        /// <summary>
        /// 判断列表是否包含指定元素
        /// </summary>
        /// <param name="item">
        /// 要查找的元素
        /// </param>
        /// <returns>
        /// 包含时返回true
        /// </returns>
        public bool Contains(T item) => _values.Contains(item);

        /// <summary>
        /// 查找元素所在索引
        /// </summary>
        /// <param name="item">
        /// 要查找的元素
        /// </param>
        /// <returns>
        /// 元素索引，未找到时返回负一
        /// </returns>
        public int IndexOf(T item) => _values.IndexOf(item);

        /// <summary>
        /// 查找第一个满足条件的元素
        /// </summary>
        /// <param name="predicate">
        /// 筛选条件
        /// </param>
        /// <returns>
        /// 匹配元素，未找到时返回默认值
        /// </returns>
        public T Find(Predicate<T> predicate) => _values.Find(predicate);

        /// <summary>
        /// 查找全部满足条件的元素
        /// </summary>
        /// <param name="predicate">
        /// 筛选条件
        /// </param>
        /// <returns>
        /// 匹配元素列表
        /// </returns>
        public List<T> FindAll(Predicate<T> predicate) => _values.FindAll(predicate);

        /// <summary>
        /// 创建当前列表的副本
        /// </summary>
        /// <returns>
        /// 新的元素列表
        /// </returns>
        public List<T> ToList() => new List<T>(_values);

        /// <summary>
        /// 获取列表枚举器
        /// </summary>
        /// <returns>
        /// 列表枚举器
        /// </returns>
        public List<T>.Enumerator GetEnumerator() => _values.GetEnumerator();

        /// <summary>
        /// 获取泛型枚举器
        /// </summary>
        /// <returns>
        /// 泛型枚举器
        /// </returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _values.GetEnumerator();

        /// <summary>
        /// 获取非泛型枚举器
        /// </summary>
        /// <returns>
        /// 非泛型枚举器
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        /// <summary>
        /// 根据批处理状态通知列表变化订阅者
        /// </summary>
        private void Notify()
        {
            //批量更新期间只记录脏状态
            if (_batchDepth > 0) { _dirty = true; return; }
            _changed?.Invoke(_values);
        }
    }

    /// <summary>
    /// 提供带变化通知的字典属性
    /// </summary>
    /// <typeparam name="TKey">
    /// 字典键类型
    /// </typeparam>
    /// <typeparam name="TValue">
    /// 字典值类型
    /// </typeparam>
    public sealed class MDictionaryProperty<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, TValue> _values;
        private int _batchDepth;
        private bool _dirty;
        private event Action<IReadOnlyDictionary<TKey, TValue>> _changed;

        /// <summary>
        /// 创建空字典属性
        /// </summary>
        public MDictionaryProperty() => _values = new Dictionary<TKey, TValue>();

        /// <summary>
        /// 使用指定比较器创建字典属性
        /// </summary>
        /// <param name="comparer">
        /// 键比较器
        /// </param>
        public MDictionaryProperty(IEqualityComparer<TKey> comparer) => _values = new Dictionary<TKey, TValue>(comparer);

        /// <summary>
        /// 获取当前键值对数量
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// 获取全部键
        /// </summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys => _values.Keys;

        /// <summary>
        /// 获取全部值
        /// </summary>
        public Dictionary<TKey, TValue>.ValueCollection Values => _values.Values;

        /// <summary>
        /// 访问指定键对应的值
        /// </summary>
        /// <param name="key">
        /// 字典键
        /// </param>
        /// <returns>
        /// 指定键对应的值
        /// </returns>
        public TValue this[TKey key]
        {
            get => _values[key];
            set { _values[key] = value; Notify(); }
        }

        /// <summary>
        /// 订阅字典变化事件
        /// </summary>
        /// <param name="listener">
        /// 字典变化时调用的回调
        /// </param>
        /// <param name="invokeImmediately">
        /// 是否先使用当前字典调用一次回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable Subscribe(Action<IReadOnlyDictionary<TKey, TValue>> listener, bool invokeImmediately = false)
        {
            if (listener == null) throw new ArgumentNullException(nameof(listener));
            if (invokeImmediately) listener(_values);
            _changed += listener;
            return new MDisposable(() => _changed -= listener);
        }

        /// <summary>
        /// 订阅字典变化事件
        /// </summary>
        /// <param name="listener">
        /// 字典变化时调用的回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable Register(Action<IReadOnlyDictionary<TKey, TValue>> listener) => Subscribe(listener);

        /// <summary>
        /// 订阅字典变化事件并立即收到当前字典
        /// </summary>
        /// <param name="listener">
        /// 字典变化时调用的回调
        /// </param>
        /// <returns>
        /// 用于取消订阅的对象
        /// </returns>
        public IDisposable RegisterWithInitValue(Action<IReadOnlyDictionary<TKey, TValue>> listener) => Subscribe(listener, true);

        /// <summary>
        /// 开始批量更新并合并后续通知
        /// </summary>
        public void BeginBatch() => _batchDepth++;

        /// <summary>
        /// 结束批量更新并按需发送一次通知
        /// </summary>
        /// <param name="notify">
        /// 是否发送合并后的通知
        /// </param>
        public void EndBatch(bool notify = true)
        {
            //嵌套批处理只在最外层结束时结算
            if (_batchDepth == 0) return;
            _batchDepth--;
            if (_batchDepth != 0) return;
            //根据脏状态决定是否发送合并通知
            var shouldNotify = _dirty && notify;
            _dirty = false;
            if (shouldNotify) _changed?.Invoke(_values);
        }

        /// <summary>
        /// 添加一个键值对
        /// </summary>
        /// <param name="key">
        /// 字典键
        /// </param>
        /// <param name="value">
        /// 字典值
        /// </param>
        public void Add(TKey key, TValue value) { _values.Add(key, value); Notify(); }

        /// <summary>
        /// 静默替换全部键值对
        /// </summary>
        /// <param name="items">
        /// 新的键值对集合
        /// </param>
        public void ReplaceSilently(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            //先清除旧数据，再写入新键值对
            _values.Clear();
            foreach (var item in items) _values[item.Key] = item.Value;
            _dirty = false;
        }

        /// <summary>
        /// 移除指定键
        /// </summary>
        /// <param name="key">
        /// 要移除的键
        /// </param>
        /// <returns>
        /// 成功移除时返回true
        /// </returns>
        public bool Remove(TKey key) { var result = _values.Remove(key); if (result) Notify(); return result; }

        /// <summary>
        /// 清空全部键值对
        /// </summary>
        public void Clear() { if (_values.Count == 0) return; _values.Clear(); Notify(); }

        /// <summary>
        /// 判断字典是否包含指定键
        /// </summary>
        /// <param name="key">
        /// 要查找的键
        /// </param>
        /// <returns>
        /// 包含时返回true
        /// </returns>
        public bool ContainsKey(TKey key) => _values.ContainsKey(key);

        /// <summary>
        /// 尝试获取指定键对应的值
        /// </summary>
        /// <param name="key">
        /// 要查找的键
        /// </param>
        /// <param name="value">
        /// 获取到的值
        /// </param>
        /// <returns>
        /// 找到键时返回true
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value) => _values.TryGetValue(key, out value);

        /// <summary>
        /// 获取字典枚举器
        /// </summary>
        /// <returns>
        /// 字典枚举器
        /// </returns>
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => _values.GetEnumerator();

        /// <summary>
        /// 获取泛型枚举器
        /// </summary>
        /// <returns>
        /// 泛型枚举器
        /// </returns>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => _values.GetEnumerator();

        /// <summary>
        /// 获取非泛型枚举器
        /// </summary>
        /// <returns>
        /// 非泛型枚举器
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

        /// <summary>
        /// 根据批处理状态通知字典变化订阅者
        /// </summary>
        private void Notify()
        {
            //批量更新期间只记录脏状态
            if (_batchDepth > 0) { _dirty = true; return; }
            _changed?.Invoke(_values);
        }
    }

    /// <summary>
    /// 将回调封装为可释放的订阅对象
    /// </summary>
    internal sealed class MDisposable : IDisposable
    {
        private Action _dispose;

        /// <summary>
        /// 创建可释放订阅对象
        /// </summary>
        /// <param name="disposeAction">
        /// 释放时执行的回调
        /// </param>
        public MDisposable(Action disposeAction) => _dispose = disposeAction;

        /// <summary>
        /// 执行释放回调并清空引用
        /// </summary>
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
}
