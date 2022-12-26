# bird.makeup

[![builds.sr.ht status](https://builds.sr.ht/~cloutier/bird.makeup/commits/master/arch.yml.svg)](https://builds.sr.ht/~cloutier/bird.makeup/commits/master/arch.yml?)

## About

Bird.makeup is a way to follow twitter user from any ActivityPub service. The aim is to make tweets appear as native a possible to the fediverse, while being as scalable as possible. Unlike BirdsiteLive, bird.makeup doesn't use official twitter api, but the undocumented frontend api, just like nitter.net, which doesn't have rate limiting. 

Most important changes from BirdsiteLive are:
 - Moved from .net core 3.1 to .net 6 which is still supported
 - Twitter API calls are not rate limited
 - There are now integration tests for the non-official api
 - Activities are now "unlisted" which means that they won't polute the public timeline

## Official instance 

You can find an official instance here: [bird.makeup](https://bird.makeup). If you are an instance admin that prefers to not have tweets federated to you, please block the entire instance. 

Please consider if you really need another instance before spinning up a new one, as having multiple domain makes it harder for moderator to block twitter content. 

## License

Original code started from [BirdsiteLive](https://github.com/NicolasConstant/BirdsiteLive).

This project is licensed under the AGPLv3 License - see [LICENSE](https://git.sr.ht/~cloutier/bird.makeup/tree/master/item/LICENSE) for details.

## Contact

You can contact me via ActivityPub <a rel="me" href="https://social.librem.one/@vincent">here</a>.


