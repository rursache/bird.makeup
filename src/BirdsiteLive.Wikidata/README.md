# Wikidata service

Wikidata is the metadata community behind Wikipedia. See for example
[Hank Green](https://www.wikidata.org/wiki/Q550996). In his page, there are 
all the links to his wikipedia pages, and many facts about him. What is 
particularly useful to us is the twitter username (P2002) and mastodon 
username (P4033). 

From this information, we can build a feature that suggests to follow the 
native fediverse account of someone you are trying to follow from Twitter.

The main downside is that those redirect are only for somewhat famous
people/organisations.

## Goals
### Being reusable by others
All this data can be useful to many other fediverse projects: tools 
for finding interesting accounts to follow, "verified" badge powered by 
Wikipedia, etc. I hope that by working on improving this dataset, we can
help other projects thrive. 
### Being independent of Twitter
Bird.makeup has to build features in a way that can't be suddenly cut off.
Building this feature with a "Log in with Twitter" is not viable.
Wikipedia is independent and outside of Elon's reach.

Also this system supports many other services: TikTok, Reddit, YouTube, etc. 
Which is really useful to expend the scope of this project while reusing as 
much work as possible
### Having great moderation

Wikipedia has many tools to help curate data and remove troll's submissions,
far better than anything I can build. I much prefer contribute to what
they are doing than try to compete