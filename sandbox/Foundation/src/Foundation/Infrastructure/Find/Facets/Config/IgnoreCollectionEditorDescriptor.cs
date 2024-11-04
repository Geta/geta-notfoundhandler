﻿using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;

namespace Foundation.Infrastructure.Find.Facets.Config
{
    public class IgnoreCollectionEditorDescriptor<T> : CollectionEditorDescriptor<T> where T : new()
    {
        public override void ModifyMetadata(ExtendedMetadata metadata, IEnumerable<Attribute> attributes)
        {
            var modelProperties = typeof(T).GetProperties();

            foreach (var property in modelProperties)
            {
                var ignoreAttribute = property.GetCustomAttributes(true).FirstOrDefault(i => i is IgnoreAttribute);

                if (ignoreAttribute != null)
                {
                    GridDefinition.ExcludedColumns.Add(property.Name);
                }
            }

            base.ModifyMetadata(metadata, attributes);
        }
    }
}
