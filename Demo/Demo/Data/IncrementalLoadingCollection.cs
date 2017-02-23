using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Demo.Data
{
    public class IncrementalLoadingCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        private readonly IEqualityComparer<T> _comparer;

        private readonly Func<int, CancellationToken, Task<IEnumerable<T>>> _load;

        private int _currentPageIndex;

        private bool _hasMoreItems = true;

        private bool _isLoading;

        public IncrementalLoadingCollection(Func<int, Task<IEnumerable<T>>> load) : this((pageIndex, cancellationToken) => load(pageIndex))
        {
            if (load == null)
            {
                throw new ArgumentNullException(nameof(load));
            }
        }

        public IncrementalLoadingCollection(Func<int, CancellationToken, Task<IEnumerable<T>>> load)
        {
            if (load == null)
            {
                throw new ArgumentNullException(nameof(load));
            }

            _load = load;
        }

        public IncrementalLoadingCollection(Func<int, Task<IEnumerable<T>>> load, IEqualityComparer<T> comparer) : this(load)
        {
            _comparer = comparer;
        }

        public IncrementalLoadingCollection(Func<int, CancellationToken, Task<IEnumerable<T>>> load, IEqualityComparer<T> comparer) : this(load)
        {
            _comparer = comparer;
        }

        public int CurrentPageIndex
        {
            get
            {
                return _currentPageIndex;
            }
            protected set
            {
                if (_currentPageIndex != value)
                {
                    _currentPageIndex = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentPageIndex)));
                }
            }
        }

        public virtual bool HasMoreItems
        {
            get
            {
                return _hasMoreItems;
            }
            protected set
            {
                if (_hasMoreItems != value)
                {
                    _hasMoreItems = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasMoreItems)));
                }
            }
        }

        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            protected set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsLoading)));
                }
            }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            if (IsLoading)
            {
                return Task.FromResult(new LoadMoreItemsResult()
                {
                    Count = 0
                }).AsAsyncOperation();
            }

            IsLoading = true;
            return AsyncInfo.Run(async cancellationToken =>
            {
                try
                {
                    var beforeLoadCount = Count;
                    var data = await _load(CurrentPageIndex, cancellationToken);
                    if (data == null)
                    {
                        HasMoreItems = false;
                    }
                    else
                    {
                        var collection = data as ICollection<T> ?? data.ToList();
                        if (collection.Count > 0)
                        {
                            CurrentPageIndex++;
                            foreach (var item in collection)
                            {
                                if (_comparer == null || this.Contains(item, _comparer) == false)
                                {
                                    Add(item);
                                }
                            }
                        }
                    }
                    var afterLoadCount = Count;

                    var resultCount = (uint)(afterLoadCount - beforeLoadCount);
                    return new LoadMoreItemsResult()
                    {
                        Count = resultCount
                    };
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        public async void Refresh()
        {
            CurrentPageIndex = 0;
            ClearItems();
            HasMoreItems = true;

            await LoadMoreItemsAsync(1);
        }
    }
}