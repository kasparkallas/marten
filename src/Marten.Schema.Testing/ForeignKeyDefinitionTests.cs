﻿using System;
using Marten.Schema.Testing.Documents;
using Marten.Storage;
using Shouldly;
using Xunit;

namespace Marten.Schema.Testing
{
    public class ForeignKeyDefinitionTests
    {
        private readonly DocumentMapping _userMapping = DocumentMapping.For<User>();
        private readonly DocumentMapping _issueMapping = DocumentMapping.For<Issue>();

        [Fact]
        public void default_key_name()
        {
            new ForeignKeyDefinition("user_id", _issueMapping, _userMapping).KeyName.ShouldBe("mt_doc_issue_user_id_fkey");
        }

        [Fact]
        public void generate_ddl()
        {
            var expected = string.Join(Environment.NewLine,
                "ALTER TABLE public.mt_doc_issue",
                "ADD CONSTRAINT mt_doc_issue_user_id_fkey FOREIGN KEY (user_id)",
                "REFERENCES public.mt_doc_user (id);");

            new ForeignKeyDefinition("user_id", _issueMapping, _userMapping).ToDDL()
                                                                            .ShouldBe(expected);
        }

        [Fact]
        public void generate_ddl_with_cascade()
        {
            var expected = string.Join(Environment.NewLine,
                "ALTER TABLE public.mt_doc_issue",
                "ADD CONSTRAINT mt_doc_issue_user_id_fkey FOREIGN KEY (user_id)",
                "REFERENCES public.mt_doc_user (id)",
                "ON DELETE CASCADE;");

            new ForeignKeyDefinition("user_id", _issueMapping, _userMapping)
                { CascadeDeletes = true }
                .ToDDL()
                .ShouldBe(expected);
        }

        [Fact]
        public void generate_ddl_on_other_schema()
        {
            var issueMappingOtherSchema = DocumentMapping.For<Issue>("schema1");
            var userMappingOtherSchema = DocumentMapping.For<User>("schema2");

            var expected = string.Join(Environment.NewLine,
                "ALTER TABLE schema1.mt_doc_issue",
                "ADD CONSTRAINT mt_doc_issue_user_id_fkey FOREIGN KEY (user_id)",
                "REFERENCES schema2.mt_doc_user (id);");

            new ForeignKeyDefinition("user_id", issueMappingOtherSchema, userMappingOtherSchema).ToDDL()
                                                                                                .ShouldBe(expected);
        }

        [Fact]
        public void generate_ddl_on_other_schema_with_cascade()
        {
            var issueMappingOtherSchema = DocumentMapping.For<Issue>("schema1");
            var userMappingOtherSchema = DocumentMapping.For<User>("schema2");

            var expected = string.Join(Environment.NewLine,
                "ALTER TABLE schema1.mt_doc_issue",
                "ADD CONSTRAINT mt_doc_issue_user_id_fkey FOREIGN KEY (user_id)",
                "REFERENCES schema2.mt_doc_user (id)",
                "ON DELETE CASCADE;");

            new ForeignKeyDefinition("user_id", issueMappingOtherSchema, userMappingOtherSchema) {CascadeDeletes = true}
                .ToDDL()
                .ShouldBe(expected);;
        }

        [Fact]
        public void generate_ddl_with_tenancy_conjoined()
        {
            _userMapping.TenancyStyle = TenancyStyle.Conjoined;
            _issueMapping.TenancyStyle = TenancyStyle.Conjoined;
            var expected = string.Join(Environment.NewLine,
                "ALTER TABLE public.mt_doc_issue",
                "ADD CONSTRAINT mt_doc_issue_user_id_tenant_id_fkey FOREIGN KEY (user_id, tenant_id)",
                "REFERENCES public.mt_doc_user (id, tenant_id);");

            new ForeignKeyDefinition("user_id", _issueMapping, _userMapping)
                .ToDDL()
                .ShouldBe(expected);
        }
    }

    public class ExternalForeignKeyDefinitionTests
    {
        private readonly DocumentMapping _userMapping = DocumentMapping.For<User>();

        [Fact]
        public void generate_ddl_without_cascade()
        {
            var expected = string.Join(Environment.NewLine,
                "ALTER TABLE public.mt_doc_user",
                "ADD CONSTRAINT mt_doc_user_user_id_fkey FOREIGN KEY (user_id)",
                "REFERENCES external_schema.external_table (external_id);");

            new ExternalForeignKeyDefinition("user_id", _userMapping,
                    "external_schema", "external_table", "external_id")
                .ToDDL()
                .ShouldBe(expected);
        }

        [Fact]
        public void generate_ddl_with_cascade()
        {
            var expected = string.Join(Environment.NewLine,
                "ALTER TABLE public.mt_doc_user",
                "ADD CONSTRAINT mt_doc_user_user_id_fkey FOREIGN KEY (user_id)",
                "REFERENCES external_schema.external_table (external_id)",
                "ON DELETE CASCADE;");

            new ExternalForeignKeyDefinition("user_id", _userMapping,
                "external_schema", "external_table", "external_id") {CascadeDeletes = true}
                .ToDDL()
                .ShouldBe(expected);
        }

        [Fact]
        public void generate_ddl_with_tenancy_conjoined()
        {
            _userMapping.TenancyStyle = TenancyStyle.Conjoined;
            var expected = string.Join(Environment.NewLine,
                "ALTER TABLE public.mt_doc_user",
                "ADD CONSTRAINT mt_doc_user_user_id_fkey FOREIGN KEY (user_id)",
                "REFERENCES external_schema.external_table (external_id);");

            new ExternalForeignKeyDefinition("user_id", _userMapping,
                    "external_schema", "external_table", "external_id")
                .ToDDL()
                .ShouldBe(expected);
        }
    }
}
