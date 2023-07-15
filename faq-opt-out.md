# What is bird.makeup and why there can't be opt-outs

You may have seen an account, let's say on mastodon, with the same handle and picture as the Twitter account of either yourself or someone you know and wondered how it got there. Let me walk you through how this works.

Anyone can start a server compatible with Mastodon and there are actually multiple choices of server that are not Mastodon but compatible with it (you may have seen it before as the "fediverse"). bird.makeup is one of such software, but it's special as it will redirect every request to Twitter and answer in a "fediverse" way. This means that every Twitter account and posts that is public is accessible through bird.makeup. 

It is open-source, which means anyone can start a copy of it and even modify their copy. As you can imagine, if a lot of people start running a copy, there will be dozens and dozens of accounts and posts that show up in search for the same Twitter handle, potentially overtaking accounts that are not proxies. This was a big problem previously with BirdsiteLive, another project that aims to have the same compatibility with Twitter, but encouraged many small instances. 

One of the main goal of bird.makeup compared to BirdsiteLive was to discourage this, and making it scalable so that there could be one "main" server. Running your own instance of bird.makeup has no upside currently over using the main instance. 

There are only a handful of alternative servers now, with people running them only because of technical interest. This has greatly reduced the load of moderators of Mastodon servers that don't like proxies, as they can now ban the address "bird.makeup" and be mostly done with it. Those that do like it are unaffected. 

Now if the main instance start having any kind of restrictions (that includes opt-out), people will now have an incentive to start new instances and we will be back to where we started. If accounts start to be blocked on "bird.makeup", dozens more will take their place under various server names.

## Are you copying all my content?

No, no tweets or media are ever stored. Everything is fetched in real-time from Twitter as fediverse server fetches it. Servers then fetch media directly from Twitter.

Bird.makeup is technically no different than alternative UI for Twitter like [nitter](https://nitter.net/).

## What can you do about it

You can [make your account private](https://twitter.com/settings/audience_and_tagging). With less data public, it is less likely proxies (from any server) will work correctly, although is it of limited usefulness. By making an account with them, you have agreed to terms of service that allow them to give (or sell) you info to anyone they wish.

One important thing to note, bird.makeup has nothing to do with Mastodon. Not the same people, organisation or even code. Mastodon's creator doesn't like the concept and the main instance is banned on servers administered by the Mastodon foundation. Please don't complain to them.

### How about GDPR?

Bird.makeup is canadian, and not bound to GDPR. Twitter itself is though since they have a EU presence. I am not a lawyer, but if there are GDPR transgressions done by Twitter giving personal data to bird.makeup, you can raise a complaint with your regulator about Twitter itself.

## What do I do about it

I run bird.makeup as a public service, and I do not wish to impersonate anyone. I add features to make clear that those accounts are proxies, and add ways in the UI to find their other fediverse accounts if they have any.

I wish there was a way to notify Twitter user of new followers from the fediverse, but since Twitter is a closed platform, I unfortunately can't.

If you have ideas of how proxies can benefit more their original creators, I am always open to those suggestions.

## Do I make money from this?

You may have noticed I have a Patreon for this project, and I am fortunate enough that it covers the cost of servers, domain, services and tools used to run it. It doesn't cover the time I spent on improving this project at all, but it's something I wanted for myself anyway and I am happy so many people find it useful. 