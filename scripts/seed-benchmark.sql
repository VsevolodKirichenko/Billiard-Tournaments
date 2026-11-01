-- Optional fixture, not application logic. Adds exactly 50,000 completed matches.
-- Existing data is retained; every execution adds a NEW fixture.
\set ON_ERROR_STOP on
BEGIN;
CREATE TEMP TABLE bench_owner AS SELECT "Id" FROM "AspNetUsers" WHERE "Email" = :'owner_email' LIMIT 1;
DO $$ BEGIN IF (SELECT count(*) FROM bench_owner) <> 1 THEN RAISE EXCEPTION 'Bootstrap administrator not found'; END IF; END $$;
CREATE TEMP TABLE bench_players AS SELECT n, gen_random_uuid() AS id FROM generate_series(1,32) n;
INSERT INTO "Players" ("Id","Name","CreatedAt") SELECT id, 'Benchmark Player ' || n, now() FROM bench_players;
CREATE TEMP TABLE bench_tournaments AS SELECT n, gen_random_uuid() AS id FROM generate_series(1,101) n;
INSERT INTO "Tournaments" ("Id","Name","OwnerId","CreatedAt","Version")
SELECT t.id, 'Benchmark Cup ' || t.n, o."Id", now(), 2 FROM bench_tournaments t CROSS JOIN bench_owner o;
CREATE TEMP TABLE bench_pairs AS
SELECT row_number() OVER (ORDER BY h.n,a.n) AS n, h.id AS home, a.id AS away
FROM bench_players h CROSS JOIN bench_players a WHERE h.n < a.n;
INSERT INTO "Matches" ("Id","TournamentId","HomeId","AwayId","HomeScore","AwayScore")
SELECT gen_random_uuid(), t.id, p.home, p.away, 8, 3 FROM bench_tournaments t CROSS JOIN bench_pairs p
WHERE (t.n - 1) * 496 + p.n <= 50000;
INSERT INTO "OutboxMessages" ("Id","TournamentId","Payload","CreatedAt","PublishedAt")
SELECT event_id, tournament_id, jsonb_build_object('EventId',event_id,'TournamentId',tournament_id,'MatchId',match_id,
    'TournamentVersion',2,'OccurredAt',now()), now(), NULL
FROM (SELECT gen_random_uuid() event_id, t.id tournament_id,
    (SELECT "Id" FROM "Matches" WHERE "TournamentId" = t.id LIMIT 1) match_id FROM bench_tournaments t) events;
COMMIT;
SELECT 'Added benchmark tournaments' AS description, count(*) AS count FROM bench_tournaments;
SELECT id AS benchmark_tournament_id FROM bench_tournaments WHERE n=1;
