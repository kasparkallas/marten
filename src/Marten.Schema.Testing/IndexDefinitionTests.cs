﻿using Marten.Schema.Testing.Documents;
using Shouldly;
using Xunit;

namespace Marten.Schema.Testing
{
    public class IndexDefinitionTests
    {
        private readonly DocumentMapping mapping = DocumentMapping.For<Target>();
        private readonly DocumentMapping mappingOhterSchema = DocumentMapping.For<Target>("other");

        [Fact]
        public void default_index_name_with_one_column()
        {
            new IndexDefinition(mapping, "long").IndexName.ShouldBe("mt_doc_target_idx_long");
        }

        [Fact]
        public void default_index_name_with_multiple_columns()
        {
            new IndexDefinition(mapping, "foo", "bar").IndexName.ShouldBe("mt_doc_target_idx_foo_bar");
        }

        [Fact]
        public void default_method_is_btree()
        {
            new IndexDefinition(mapping, "foo").Method.ShouldBe(IndexMethod.btree);
        }

        [Fact]
        public void default_sort_order_is_asc()
        {
            new IndexDefinition(mapping, "foo").SortOrder.ShouldBe(SortOrder.Asc);
        }

        [Fact]
        public void default_unique_is_false()
        {
            new IndexDefinition(mapping, "foo").IsUnique.ShouldBeFalse();
        }

        [Fact]
        public void default_concurrent_is_false()
        {
            new IndexDefinition(mapping, "foo").IsConcurrent.ShouldBeFalse();
        }

        [Fact]
        public void default_modifier_is_null()
        {
            SpecificationExtensions.ShouldBeNull(new IndexDefinition(mapping, "foo").Modifier);
        }

        [Fact]
        public void generate_ddl_for_single_column_all_defaults()
        {
            new IndexDefinition(mapping, "foo").ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON public.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_other_database_schema_for_single_column_all_defaults()
        {
            new IndexDefinition(mappingOhterSchema, "foo").ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON other.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_for_custom_index_name()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.IndexName = "bar";

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_bar ON public.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_for_descending_sort_order()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.SortOrder = SortOrder.Desc;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON public.mt_doc_target (\"foo\" DESC);");
        }

        [InlineData(IndexMethod.btree, true)]
        [InlineData(IndexMethod.gin, false)]
        [InlineData(IndexMethod.brin, false)]
        [InlineData(IndexMethod.gist, false)]
        [InlineData(IndexMethod.hash, false)]
        [Theory]
        public void sort_order_only_applied_for_btree_index(IndexMethod method, bool shouldUseSort)
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.Method = method;
            definition.SortOrder = SortOrder.Desc;

            var ddl = definition.ToDDL();

            if (shouldUseSort)
            {
                ddl.ShouldEndWith(" DESC);");
            }
            else
            {
                ddl.ShouldNotEndWith(" DESC);");
            }
        }

        [Fact]
        public void generate_ddl_for_single_column_unique()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.IsUnique = true;

            definition.ToDDL()
                .ShouldBe("CREATE UNIQUE INDEX mt_doc_target_idx_foo ON public.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_other_database_schema_for_single_column_unique()
        {
            var definition = new IndexDefinition(mappingOhterSchema, "foo");
            definition.IsUnique = true;

            definition.ToDDL()
                .ShouldBe("CREATE UNIQUE INDEX mt_doc_target_idx_foo ON other.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_with_modifier()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.IsUnique = true;
            definition.Modifier = "WITH (fillfactor = 70)";

            definition.ToDDL()
                .ShouldBe("CREATE UNIQUE INDEX mt_doc_target_idx_foo ON public.mt_doc_target (\"foo\") WITH (fillfactor = 70);");
        }

        [Fact]
        public void generate_ddl_other_database_schema_with_modifier()
        {
            var definition = new IndexDefinition(mappingOhterSchema, "foo");
            definition.IsUnique = true;
            definition.Modifier = "WITH (fillfactor = 70)";

            definition.ToDDL()
                .ShouldBe("CREATE UNIQUE INDEX mt_doc_target_idx_foo ON other.mt_doc_target (\"foo\") WITH (fillfactor = 70);");
        }

        [Fact]
        public void generate_ddl_for_concurrent_index()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.IsConcurrent = true;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX CONCURRENTLY mt_doc_target_idx_foo ON public.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_other_database_schema_for_concurrent_index()
        {
            var definition = new IndexDefinition(mappingOhterSchema, "foo");
            definition.IsConcurrent = true;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX CONCURRENTLY mt_doc_target_idx_foo ON other.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_for_concurrent_unique_index()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.IsConcurrent = true;
            definition.IsUnique = true;

            definition.ToDDL()
                .ShouldBe("CREATE UNIQUE INDEX CONCURRENTLY mt_doc_target_idx_foo ON public.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_other_database_schema_for_concurrent_unique_index()
        {
            var definition = new IndexDefinition(mappingOhterSchema, "foo");
            definition.IsConcurrent = true;
            definition.IsUnique = true;

            definition.ToDDL()
                .ShouldBe("CREATE UNIQUE INDEX CONCURRENTLY mt_doc_target_idx_foo ON other.mt_doc_target (\"foo\");");
        }

        [Fact]
        public void generate_ddl_for_gin_index()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.Method = IndexMethod.gin;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON public.mt_doc_target USING gin (\"foo\");");
        }

        [Fact]
        public void generate_ddl_other_database_schema_for_gin_index()
        {
            var definition = new IndexDefinition(mappingOhterSchema, "foo");
            definition.Method = IndexMethod.gin;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON other.mt_doc_target USING gin (\"foo\");");
        }

        [Fact]
        public void generate_ddl_for_gin_with_jsonb_path_ops_index()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.Method = IndexMethod.gin;
            definition.Expression = "? jsonb_path_ops";

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON public.mt_doc_target USING gin (\"foo\" jsonb_path_ops);");
        }

        [Fact]
        public void generate_ddl_other_database_schema_for_gin_with_jsonb_path_ops_index()
        {
            var definition = new IndexDefinition(mappingOhterSchema, "foo");
            definition.Method = IndexMethod.gin;
            definition.Expression = "? jsonb_path_ops";

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON other.mt_doc_target USING gin (\"foo\" jsonb_path_ops);");
        }

        [Fact]
        public void generate_ddl_for_gist_index()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.Method = IndexMethod.gist;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON public.mt_doc_target USING gist (\"foo\");");
        }

        [Fact]
        public void generate_ddl_other_database_schema_for_gist_index()
        {
            var definition = new IndexDefinition(mappingOhterSchema, "foo");
            definition.Method = IndexMethod.gist;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON other.mt_doc_target USING gist (\"foo\");");
        }

        [Fact]
        public void generate_ddl_for_hash_index()
        {
            var definition = new IndexDefinition(mapping, "foo");
            definition.Method = IndexMethod.hash;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON public.mt_doc_target USING hash (\"foo\");");
        }

        [Fact]
        public void generate_ddl_on_other_database_schema_for_hash_index()
        {
            var definition = new IndexDefinition(mappingOhterSchema, "foo");
            definition.Method = IndexMethod.hash;

            definition.ToDDL()
                .ShouldBe("CREATE INDEX mt_doc_target_idx_foo ON other.mt_doc_target USING hash (\"foo\");");
        }
    }
}
