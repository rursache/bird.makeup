# bird.makeup

[![builds.sr.ht status](https://builds.sr.ht/~cloutier/bird.makeup/commits/master/arch.yml.svg)](https://builds.sr.ht/~cloutier/bird.makeup/commits/master/arch.yml?)

## About

Bird.makeup is a way to follow Twitter users from any ActivityPub service. The aim is to make tweets appear as native a possible to the fediverse, while being as scalable as possible. The project started from BirdsiteLive, but has now been improved significantly. 

Compared to BirdsiteLive, bird.makeup is:

More modern:
 - Moved from .net core 3.1 to .net 6 which is still supported
 - Moved from postgres 9 to 15
 - Moved from Newtonsoft.Json to System.Text.Json

More scalable:
 - Twitter API calls are not rate-limited
 - There are now integration tests for the non-official api
 - The core pipeline has been tweaked to remove bottlenecks. As of writing this, bird.makeup supports without problems more than 10k users. 
 - Twitter users with no followers on the fediverse will stop being fetched

More native to the fediverse:
 - Retweets are propagated as boosts
 - Activities are now "unlisted" which means that they won't polute the public timeline
 - WIP support for QT

## Official instance 

You can find an official instance here: [bird.makeup](https://bird.makeup). If you are an instance admin that prefers to not have tweets federated to you, please block the entire instance. 

Please consider if you really need another instance before spinning up a new one, as having multiple domain makes it harder for moderators to block twitter content. 

## License

Original code started from [BirdsiteLive](https://github.com/NicolasConstant/BirdsiteLive).

This project is licensed under the AGPLv3 License - see [LICENSE](https://git.sr.ht/~cloutier/bird.makeup/tree/master/item/LICENSE) for details.

## Contact

You can contact me via ActivityPub <a rel="me" href="https://social.librem.one/@vincent">here</a>.


