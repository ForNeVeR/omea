// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace JetBrains.Omea.OpenAPI
{
    public class BusinessObject
    {
        private IResource _resource;
        private string _newResourceType;
        private Dictionary<int, object> _props;

        protected BusinessObject(string resourceType)
        {
            _newResourceType = resourceType;
        }

        protected BusinessObject(IResource res)
        {
            _resource = res;
        }

        public IResource Resource
        {
            get { return _resource; }
        }

        public T GetProp<T>(PropId<T> propId)
        {
            lock(this)
            {
                if (_props != null)
                {
                    return (T)_props[propId.Id];
                }
                if (_resource != null)
                {
                    return _resource.GetProp(propId);
                }
            }
            return default(T);
        }

        public void SetProp<T>(PropId<T> propId, T value)
        {
            lock(this)
            {
                if (_resource != null && Core.ResourceAP.IsOwnerThread)
                {
                    _resource.SetProp(propId, value);
                }
                else
                {
                    if (_props == null)
                    {
                        _props = new Dictionary<int, object>();
                    }
                    _props[propId.Id] = value;
                }
            }
        }

        public bool HasProp<T>(PropId<T> propId)
        {
            lock(this)
            {
                return (_props != null && _props.ContainsKey(propId.Id)) ||
                       (_resource != null && _resource.HasProp(propId.Id));
            }
        }

        public void Save()
        {
            if (_props != null || _newResourceType != null)
            {
                Core.ResourceAP.RunJob("Saving BusinessObject", DoSave);
            }
        }

        public void SaveAsync()
        {
            if (_props != null || _newResourceType != null)
            {
                Core.ResourceAP.QueueJob("Saving BusinessObject", DoSave);
            }
        }

        private void DoSave()
        {
            lock(this)
            {
                if (_resource == null)
                {
                    _resource = Core.ResourceStore.BeginNewResource(_newResourceType);
                    BusinessObjectCache.Put(this);
                }
                else
                {
                    _resource.BeginUpdate();
                }
                if (_props != null)
                {
                    foreach (var prop in _props)
                    {
                        _resource.SetProp(prop.Key, prop.Value);
                    }
                    _props = null;
                }
                _resource.EndUpdate();
            }
        }

        public void Delete()
        {
            int id = _resource.Id;
            if (Core.ResourceAP.IsOwnerThread)
            {
                _resource.Delete();
            }
            else
            {
                new ResourceProxy(_resource).Delete();
            }
            BusinessObjectCache.Remove(id);
        }
    }

    public abstract class ResourceTypeId<T> where T: BusinessObject
    {
        private readonly string _name;

        protected ResourceTypeId(string name)
        {
            _name = name;
        }

        public string Name
        {
            get { return _name; }
        }

        public abstract T CreateBusinessObject(IResource res);
    }

    public class BusinessObjectCache
    {
        private static readonly Dictionary<int, WeakReference> _cache = new Dictionary<int,WeakReference>();

        public static T Get<T>(IResource res, ResourceTypeId<T> resourceTypeId) where T: BusinessObject
        {
            lock(_cache)
            {
                WeakReference reference;
                if (_cache.TryGetValue(res.Id, out reference))
                {
                    object target = reference.Target;
                    if (target != null)
                    {
                        return (T) target;
                    }
                }
                T result = resourceTypeId.CreateBusinessObject(res);
                _cache[res.Id] = new WeakReference(result);
                return result;
            }
        }

        internal static void Put(BusinessObject o)
        {
            lock(_cache)
            {
                _cache [o.Resource.Id] = new WeakReference(o);
            }
        }

        internal static void Remove(int id)
        {
            lock(_cache)
            {
                _cache.Remove(id);
            }
        }
    }

    public class BusinessObjectList<T>: IEnumerable<T> where T: BusinessObject
    {
        private readonly ResourceTypeId<T> _resourceTypeId;
        private readonly IResourceList _baseList;

        public BusinessObjectList(ResourceTypeId<T> resourceTypeId, IResourceList baseList)
        {
            _resourceTypeId = resourceTypeId;
            _baseList = baseList;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>) this).GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new BusinessObjectEnumerator<T>(_resourceTypeId, _baseList.ValidResources.GetEnumerator());
        }

        public int Count
        {
            get { return _baseList.Count; }
        }

        public T this[int i]
        {
            get { return BusinessObjectCache.Get(_baseList[i], _resourceTypeId); }
        }

        public BusinessObjectList<T> Intersect(BusinessObjectList<T> list)
        {
            return new BusinessObjectList<T>(_resourceTypeId, _baseList.Intersect(list._baseList));
        }

        class BusinessObjectEnumerator<T>: IEnumerator<T> where T: BusinessObject
        {
            private readonly ResourceTypeId<T> _resourceType;
            private readonly IEnumerator _resourceEnumerator;

            public BusinessObjectEnumerator(ResourceTypeId<T> resourceType, IEnumerator idEnumerator)
            {
                _resourceType = resourceType;
                _resourceEnumerator = idEnumerator;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                return _resourceEnumerator.MoveNext();
            }

            public void Reset()
            {
                _resourceEnumerator.Reset();
            }

            object IEnumerator.Current
            {
                get { return GetCurrentItem(); }
            }

            public T Current
            {
                get { return GetCurrentItem(); }
            }

            private T GetCurrentItem()
            {
                IResource res = (IResource) _resourceEnumerator.Current;
                return BusinessObjectCache.Get(res, _resourceType);
            }
        }
    }


}
