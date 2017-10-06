using System;
using Impression;

namespace Example07SkinnedModel
{
    class Program
    {
        static void Main(string[] args)
		{
			using(var game = new Example07SkinnedModelGame())
            {
                game.Run();
            }
		}
    }
}