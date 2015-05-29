using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace SaveAllTheTabs
{
    public class DocumentGroup : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public byte[] Positions { get; set; }

        [JsonIgnore]
        public bool IsEditing
        {
            get { return _isEditing; }
            set { SetField(ref _isEditing, value); }
        }
        private bool _isEditing;

        public string Name
        {
            get { return _name; }
            set { SetField(ref _name, value); }
        }
        private string _name;

        public int? Slot
        {
            get { return _slot; }
            set
            {
                if (SetField(ref _slot, value))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSlot)));
                }
            }
        }
        private int? _slot;

        public string Description
        {
            get { return _description; }
            set
            {
                if (SetField(ref _description, value))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
                }
            }
        }
        private string _description;

        public HashSet<string> Files { get; set; }

        [JsonIgnore]
        public bool HasSlot => Slot != null;

        [JsonIgnore]
        public bool IsStash => Name.Equals(DocumentManager.StashGroupName, StringComparison.InvariantCultureIgnoreCase);

        [JsonIgnore]
        public bool IsSelected { get; set; }

        public void StartEditing()
        {
            IsEditing = true;
        }

        public string EndEditing()
        {
            IsEditing = false;
            return Name;
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}