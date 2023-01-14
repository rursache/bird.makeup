
# Most common servers

```SQL
SELECT COUNT(*), host FROM followers GROUP BY host ORDER BY count DESC;
```

# Most popular twitter users

```SQL
SELECT COUNT(*), acct FROM (SELECT unnest(followings) as follow FROM followers) AS f INNER JOIN twitter_users ON f.follow=twitter_users.id GROUP BY acct ORDER BY count DESC;
```

```SQL
SELECT COUNT(*), acct, id FROM (SELECT unnest(followings) as follow FROM followers) AS f INNER JOIN twitter_users ON f.follow=twitter_users.id WHERE id IN ( SELECT unnest(followings) FROM followers WHERE host='social.librem.one' AND acct = 'vincent' ) GROUP BY acct, id ORDER BY count DESC;
```

# Most active users

```SQL
SELECT array_length(followings, 1) AS l, acct, host FROM followers ORDER BY l DESC;
```

# Lag

```SQL
SELECT COUNT(*), date_trunc('day', lastsync) FROM (SELECT unnest(followings) as follow FROM followers GROUP BY follow) AS f INNER JOIN twitter_users ON f.follow=twitter_users.id GROUP BY date_trunc;

SELECT COUNT(*), date_trunc('hour', lastsync) FROM (SELECT unnest(followings) as follow FROM followers GROUP BY follow) AS f INNER JOIN twitter_users ON f.follow=twitter_users.id GROUP BY date_trunc ORDER BY date_trunc;
```

