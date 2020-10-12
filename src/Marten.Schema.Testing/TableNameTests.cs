﻿using Shouldly;
using Xunit;

namespace Marten.Schema.Testing
{
    public class TableNameTests
    {
        [Fact]
        public void owner_name_has_no_schema_if_schema_is_public()
        {
            var table = new DbObjectName("public", "mt_doc_user");
            table.OwnerName.ShouldBe("mt_doc_user");
        }

        [Fact]
        public void owner_name_has_schema_if_not_in_public()
        {
            var table = new DbObjectName("other", "mt_doc_user");
            table.OwnerName.ShouldBe("other.mt_doc_user");
        }

        [Fact]
        public void can_parse_without_schema()
        {
            var table = DbObjectName.Parse("mt_doc_user");
            table.Name.ShouldBe("mt_doc_user");
            table.Schema.ShouldBe("public");
            table.QualifiedName.ShouldBe("public.mt_doc_user");
        }
    }
}