using Genesis.AI.Domain.AggregatesModel.ProjectAggregate;

namespace Genesis.AI.Domain.Interfaces;

public interface IProjectMarkdownGenerator
{
    string Generate(Project project);
}
