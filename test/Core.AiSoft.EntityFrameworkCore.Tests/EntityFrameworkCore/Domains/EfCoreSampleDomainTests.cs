using Core.AiSoft.Samples;
using Xunit;

namespace Core.AiSoft.EntityFrameworkCore.Domains;

[Collection(AiSoftTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<AiSoftEntityFrameworkCoreTestModule>
{

}
