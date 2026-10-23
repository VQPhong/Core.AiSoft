using Xunit;

namespace Core.AiSoft.EntityFrameworkCore;

[CollectionDefinition(AiSoftTestConsts.CollectionDefinitionName)]
public class AiSoftEntityFrameworkCoreCollection : ICollectionFixture<AiSoftEntityFrameworkCoreFixture>
{

}
