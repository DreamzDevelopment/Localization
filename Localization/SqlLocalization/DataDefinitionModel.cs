using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Localization.SqlLocalizer
{
    public class EntityListModel
    {
        public virtual int Id { get; set; }
        [StringLength(100, ErrorMessage = "Maximum number of characters allowed is 100")]
        public virtual string EntityName { get; set; } // Table/Entity Name in the Context
        public virtual string FullQualifiedName { get; set; } // Table/Entity Name in the Context
        public virtual IEnumerable<EntityProperty> EntityProperties { get; set; } // Selected fields of Table/Entity
    }   
    public class EntityProperty 
    {
        public virtual int Id { get; set; }
        [StringLength(100, ErrorMessage = "Maximum number of characters allowed is 100")]
        public virtual string PropertyName { get; set; } // Selected field name of Table/Entity
        public virtual IEnumerable<PropertyValue> PropertyValues { get; set; } // Key value pair-for each attribute type of Table/Entity
    }
    public class PropertyValue 
    {
        public PropertyValue() { }
        public PropertyValue(int id, string key, string value, string translatedValue = null) { 
            this.Id = id;
            this.Key = key;
            this.Value = value;
            this.TranslatedValue = translatedValue;
        }
        public virtual int Id { get; set; }
        public virtual string Key { get; set; } // Attribute Identity 
        public virtual string Value { get; set; } // Attribute Value
        public virtual string TranslatedValue { get; set; } // Attribute Value
    }
    public class PropertyData 
    {
        public virtual int Id { get; set; }
        public virtual string PropertyName { get; set; } // Attributes Group/Parent Name
        public virtual ICollection<PropertyValue> PropertyValues { get; set; } // Attribute Identity + Attribute Value
    }
}