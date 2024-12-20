using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Repositories
{
    public class YoutubeVideoRepository : Repository<YoutubeVideo>, IYoutubeVideoRepository
    {
        public YoutubeVideoRepository(SoftbreakContext context) : base(context)
        {
        }

        // YoutubeVideo'ya özel metotlar burada uygulanabilir.
    }
}