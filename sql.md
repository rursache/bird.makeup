
# Most common servers

```SQL
SELECT COUNT(*), host FROM followers GROUP BY host ORDER BY count DESC;
```

# Most popular twitter users

```SQL
SELECT COUNT(*), acct FROM (SELECT unnest(followings) as follow FROM followers) AS f INNER JOIN twitter_users ON f.follow=twitter_users.id GROUP BY acct ORDER BY count DESC;
```

# Most active users

```SQL
SELECT array_length(followings, 1) AS l, acct, host FROM followers ORDER BY l DESC;
```

# Lag

```SQL
SELECT COUNT(*), date_trunc('day', lastsync) FROM (SELECT unnest(followings) as follow FROM followers GROUP BY follow) AS f INNER JOIN twitter_users ON f.follow=twitter_users.id GROUP BY date_trunc;
```
