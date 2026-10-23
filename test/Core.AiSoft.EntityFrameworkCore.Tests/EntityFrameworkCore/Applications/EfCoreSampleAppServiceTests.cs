using Core.AiSoft.Samples;
using Xunit;

namespace Core.AiSoft.EntityFrameworkCore.Applications;

[Collection(AiSoftTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<AiSoftEntityFrameworkCoreTestModule>
{

}
