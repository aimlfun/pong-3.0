# pong-3.0

From the blog post: https://aimlfun.com/a-better-pong/

As it runs, it spools the "training data" to c:\temp\pong.txt. It also learns. playing in an automated manner. By 3000 epoch's the only time it generally misses is when the ball ends up in the intentional unreachable "region" (dead zone).

Please note: this is part of my blog on Pong. The codebase proves it can move the bat to deflect the ball after "learning". It *isn't* designed to beat opponenents, as once trained it will always hit the ball perpendicular where possible (a poor strategy). The next level post will achieve the same behaviour seeing the video display and subsequently I hope to post something combining a targeting strategy (learning to "place" the ball to win).

Pong is trickier than you might think. This page explains AI challenges with Pong: https://aimlfun.com/pong/
