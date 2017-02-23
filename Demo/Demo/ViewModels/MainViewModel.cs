using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Demo.Data;
using Demo.Models;
using GalaSoft.MvvmLight;

namespace Demo.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private bool _isBusy;

        public MainViewModel()
        {
            Persons = new IncrementalLoadingCollection<Person>(LoadPersonsAsync);
        }

        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            private set
            {
                Set(ref _isBusy, value);
            }
        }

        public IncrementalLoadingCollection<Person> Persons
        {
            get;
        }

        private async Task<IEnumerable<Person>> LoadPersonsAsync(int pageIndex, CancellationToken cancellationToken)
        {
            if (IsBusy)
            {
                return Enumerable.Empty<Person>();
            }

            IsBusy = true;
            try
            {
                await Task.Delay(3000, cancellationToken);
                var persons = new List<Person>();
                for (var i = pageIndex; i < pageIndex + 10; i++)
                {
                    persons.Add(new Person()
                    {
                        Name = "Name" + i,
                        Age = i
                    });
                }
                return persons;
            }
            catch (Exception)
            {
                return Enumerable.Empty<Person>();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}