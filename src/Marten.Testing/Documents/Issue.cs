using System;

namespace Marten.Testing.Documents
{
    // SAMPLE: Issue
    public class Issue
    {
        public Issue()
        {
            Id = Guid.NewGuid();
        }

        public string[] Tags { get; set; }

        public Guid Id { get; set; }

        public string Title { get; set; }

        public int Number { get; set; }

        public Guid? AssigneeId { get; set; }

        public Guid? ReporterId { get; set; }

        public Guid? BugId { get; set; }
    }

    // ENDSAMPLE

    // SAMPLE: Bug
    public class Bug
    {
        public Bug()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Title { get; set; }

        public int IssueTrackerId { get; set; }
    }

    // ENDSAMPLE
}
