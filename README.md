<p align="center">
  <img src="MiniGamesEmporium/Images/minigamesemporium.png" alt="Mini Games Emporium" width="480"/>
</p>

<h1 align="center">Mini Games Emporium</h1>

<p align="center">
  A Dalamud plugin for Final Fantasy XIV, the official mini game hub for OOF Games venues.
</p>

---

## Overview

**Mini Games Emporium** is the all-in-one hosting tool powering every mini game run at OOF Games. It handles everything a host needs: player queues, session tracking, trade verification, automated chat announcements, and a full transaction ledger, so the host can stay focused on the crowd, not the spreadsheet.

The plugin lives inside FFXIV via the [Dalamud](https://github.com/goatcorp/Dalamud) framework and is accessed with `/mge` or `/minigamesemporium`.

### What's Inside

| Game | Status |
|---|---|
| **BAR 777** | ✅ Live |
| **Deathroll Tournament** | ✅ Live |
| **Higher/Lower** | ✅ Live |
| Minefield Gambit | 🔜 Coming Soon |
| Gambler Derby | 🔜 Coming Soon |
| Beer Pong | 🔜 Coming Soon |
| Darts | 🔜 Coming Soon |
| 8 Ball Pool | 🔜 Coming Soon |
| Russian Roulette | 🔜 Coming Soon |
| Raid Boss | 🔜 Coming Soon |
| Deal or No Deal | 🔜 Coming Soon |
| Voting Madness | 🔜 Coming Soon |

Each game gets its own tab inside the plugin's **Mini Games** section, styled in its own neon accent colour. The main window also holds a **Transaction History** ledger and a top-level **Settings** tab.

---

## BAR 777

BAR 777 is OOF Games' flagship game. Players pay an entry fee, then use FFXIV's built-in `/random` command to roll for the winning number. Hit it within the allotted rolls and you take home the pot.

### How It Works

1. The host opens the BAR 777 door, sets the entry cost, roll count, and winning number, then clicks **Start Session** (green button).
2. A player is pulled from the queue (or walks in) and the host targets them to lock them in.
3. The host clicks **Trade** to open a trade, and the plugin waits for the player's Gil to come through.
4. Once payment is verified, the host sends the start message and the player begins rolling `/random`.
5. The plugin watches chat in real time. Every roll is logged and compared against the winning number.
6. If the player hits the number, a win is detected automatically and a shout goes out. If they exhaust their rolls, an unlucky message fires and the session closes.
7. **End Session & Process Next** advances the queue and the next player is pulled in automatically.

### Entry Modes

#### Walk-in Mode

Walk-in is the default. There is no queue; players approach the host directly and are added to the session on the spot. The game tab goes full-width and shows a permanent **End Session** button. Ideal for low-traffic events or drop-in style nights.

#### Queue Mode

Queue mode activates an automatic waitlist. Players type a configured keyword (e.g. `!join`) in say, shout, yell, or tell and are added to the queue instantly. The plugin sends them a tell confirming their position.

The right-hand column of the Game tab shows the live queue at all times:

- **Current** - the active player with controls (To Back Q / Remove / End Session & Process Next)
- **Waiting list** - everyone else in order, with a manual add field at the bottom
- The queue persists across plugin restarts, so a crash mid-event doesn't wipe the list

When the active session ends normally, **End Session & Process Next** removes that player from the queue head and starts the next one automatically. **Stop Session** clears only the active session and leaves the waitlist untouched.

### Statistics Panel

The Statistics panel at the bottom of the Game tab shows the live pot and session figures:

| Row | Description |
|---|---|
| **Total Pot** | Boosted Pot + the configured share of trade revenue; the **Announce Pot** button shouts it on demand |
| **Boosted Pot** | Extra Gil added by the venue on top of trade revenue |
| **Taken in Trades** | Total Gil collected from players this session |
| **Kept from Trades** | Portion of trade revenue held back for the venue (shown only when Trades to Pot is below 100%) |
| **Players Played** | Number of players who have played this session |
| **In Queue** | Players waiting to play (queue mode only) |

**Trades to Pot (%)**, set on the door, controls how much of the trade revenue feeds the pot: `Total Pot = Boosted Pot + (Taken in Trades × Trades to Pot %)`. Whatever is not fed to the pot shows as Kept from Trades. The **Adjust Pot (Gil)** row lets the host add or remove Gil from the pot directly at any time.

### Automated Chat

BAR 777 includes a full set of customisable message templates, each editable in the Chat tab with toggleable auto-send where noted:

| Template | Trigger |
|---|---|
| Rules | Manually via **Send Rules** on the Game tab |
| Request Gil | Manually from the Game tab |
| Request Gil (Buyer) | Manually, when someone else is paying for the player |
| Start Rolls | When payment is verified (auto-send toggle) |
| Halfway | At the halfway point of the player's rolls (auto-send toggle) |
| Unlucky | When all rolls are exhausted with no win (auto-send toggle) |
| Win Shout | On win detection, includes the total pot (auto-send toggle) |
| Announce Pot | Posts the current pot on demand |
| Announce Keyword | Shouts the join keyword to invite players (queue mode) |
| Join Queue | Tells a player their position when they join (auto-send toggle) |
| Reminder to Play | Reminds upcoming players once the queue reaches a set size (auto-send toggle) |
| Next Player Up | Announces the next player in the queue |

A **channel selector** (Say / Party / Alliance) sits above the templates and rewrites the leading channel prefix of every message in one click, switching the whole set between `/say`, `/party`, and `/alliance` at once. Dedicated `/tell`, `/yell`, and `/shout` messages keep their own channel.

All outbound messages are queued with a one-second gap between each to stay within FFXIV's chat rate limits.

### Screenshots

> **BAR 777 Door (Pre-Session)**
>
> ![BAR 777 Door (Pre-Session)](MiniGamesEmporium/Images/Screenshots/start-session-tab.png?v=2)

---
> **Game Tab: Queue Mode**
>
> ![Game Tab: Queue Mode](MiniGamesEmporium/Images/Screenshots/queue-mode.png?v=2)

---

> **Game Tab: Walk-in Mode**
>
> ![Game Tab: Walk-in Mode](MiniGamesEmporium/Images/Screenshots/walk-in-mode.png?v=2)

---

> **Chat Settings Tab**
>
> ![Chat Settings Tab](MiniGamesEmporium/Images/Screenshots/chat-settings.png?v=2)

---

## Deathroll Tournament

Deathroll Tournament is a bracket-based elimination game. Each registered player pays an entry fee and competes in head-to-head deathroll matches. In deathroll, each player rolls within the range of the previous roll, starting from an agreed ceiling. The first player to roll a 1 loses the match. The plugin manages registration, seeding, the bracket, and all automated chat, so the host only needs to click to advance.

### How It Works

1. The host opens the Deathroll Tournament door, sets the entry cost, boosted pot, and best-of series lengths for each round, then clicks **Start Session**.
2. Players register by typing the join keyword in chat; the host marks each as **Paid** in the Registration tab once their Gil comes through.
3. When all paid players are confirmed, the host clicks **Start Tournament** to seed the bracket automatically (power-of-two seeding; bye slots are auto-resolved).
4. The plugin announces the first matchup and uses `/random 10` to determine which player rolls first.
5. Players deathroll in order. The plugin watches for the losing roll (a result of 1) and auto-advances the match.
6. The winner advances in the bracket; the next match is announced automatically.
7. The tournament ends when one player remains. A winner announcement fires and the Discord embed updates to show the champion.

### Registration

The Registration tab shows every player who has typed the join keyword. The host ticks each player as **Paid** once their trade clears. Only paid players are seeded into the bracket.

- **Entry Cost** - Gil each paid player owes the host.
- **Boosted Pot** - Extra Gil added by the venue on top of entry fees.
- **Registered** - All players who joined, including those not yet marked as paid.
- **Paid** - Players confirmed for the bracket.

### The Bracket

Once started, the Bracket tab shows the full single-elimination bracket in real time. Matches highlight the current pairing; completed matches show the winner in green. Bye slots are resolved instantly with no announcement.

The bracket is seeded in a standard power-of-two layout. If the paid player count is not a power of two, the plugin fills the smallest number of first-round byes required to reach the next power of two.

### Best-of Series

Each round can be configured with its own best-of count before the tournament starts (for example, Best of 1 in early rounds, Best of 3 in the semi-finals, Best of 5 in the final). The plugin tracks wins per match and only advances a player when they reach the winning threshold.

### Statistics Panel

The Statistics panel at the bottom of the Game tab shows the live pot and tournament figures:

| Row | Description |
|---|---|
| **Total Pot** | Entry fees from all paid players + Boosted Pot; the **Announce Pot** button shouts it on demand |
| **Entry Cost** | Gil each paid player owes |
| **Boosted Pot** | Extra Gil added by the venue on top of entry fees |
| **Players** | Number of paid players in the tournament |
| **Round** | The current round, e.g. "Round 2 of 4" |

The **Adjust Pot (Gil)** row lets the host add or remove Gil from the pot directly at any time.

### Automated Chat

Deathroll Tournament includes a full set of customisable message templates, each editable in the Chat tab with toggleable auto-send where noted:

| Template | Trigger |
|---|---|
| Request Gil | Manually per player from the Registration table |
| Request Gil (Buyer) | Manually, when someone else is paying a player's entry |
| Announce Bracket | When the bracket is generated; posts the matchups |
| Announce Matchup | At the start of each match (auto-send toggle) |
| First Player | After `/random 10` determines who rolls first (auto-send toggle) |
| Re-roll Random 10 | When both players tie their `/random 10` and must roll again (auto-send toggle) |
| Announce Round Win | When a player wins a single game but the series continues (auto-send toggle) |
| Announce Match Win | When a player wins the series and their opponent is eliminated (auto-send toggle) |
| Announce Winner | When the final match resolves and the tournament winner is decided (auto-send toggle) |
| Announce Pot | Posts the current pot on demand |

All outbound messages are queued with a one-second gap between each to stay within FFXIV's chat rate limits.

### Discord Webhook

Deathroll Tournament can post and live-update a single Discord embed via a channel webhook throughout the event.

To set it up, go to the **Discord** tab within the Deathroll Tournament panel:

1. In Discord, open channel settings for your tournament announcement channel, go to **Integrations > Webhooks**, and copy the webhook URL.
2. Paste the URL into the Webhook URL field and tick **Enable**.
3. The plugin will post the embed immediately and patch it automatically as the tournament progresses.

| Phase | Embed content |
|---|---|
| No session active | "No Tournament Active" banner with the Deathroll Tournament logo |
| Registration open | Player card showing all paid players, entry cost, and current pot |
| Tournament running | Live bracket image with current match and score highlighted |
| Tournament complete | Final bracket image with the winner and pot total |

If the Discord message is deleted, toggle **Enable** off then on to create a fresh embed. The same toggle retries a failed delivery.

### Screenshots

> **Session Tab (Pre-Session)**
>
> ![Session Tab (Pre-Session)](MiniGamesEmporium/Images/Screenshots/drt-session-tab.png)

---

> **Lobby (Registration)**
>
> ![Lobby (Registration)](MiniGamesEmporium/Images/Screenshots/drt-lobby.png)

---

> **Bracket**
>
> ![Bracket](MiniGamesEmporium/Images/Screenshots/drt-bracket.png)

---

> **Example: Live Bracket Embed (Discord)**
>
> ![Example: Live Bracket Embed (Discord)](MiniGamesEmporium/Images/Screenshots/drt-example-bracket.png)

---

## Higher/Lower

Higher/Lower is a party-based streak game. The host rolls a dice with `/dice`, and the player calls whether the next roll will be higher or lower. Every correct call extends their round streak; one wrong call ends their turn. Multiple players each take a turn in the same session, and whoever builds the longest streak takes the pot. The plugin reads the rolls and the player's guess straight from chat, tracks a live leaderboard, and handles the winner payout.

### How It Works

1. The host opens the Higher/Lower door, sets the entry cost, boosted pot, dice size, and win options, then clicks **Start Session**.
2. The host invites the player to their party so they can see the dice rolls, then selects them from the party list.
3. The host collects the entry cost via trade; once payment is verified the turn begins.
4. The host rolls `/dice X` (the configured dice size) to set the opening number.
5. The player calls **Higher** or **Lower** for the next roll, either by typing it in party chat or by the host clicking the button.
6. The host rolls again. A correct call increases the round count and play continues; a wrong call ends the turn. Rolling the same number is a no-op, so the host simply rolls once more.
7. When a turn ends, the player's best streak is recorded on the leaderboard. The next player is pulled in and paid up the same way.
8. Once everyone has played, the host clicks **Finish Game** to lock in the winner(s) and pay out the pot.

### Game Options

Set on the door before the session starts:

| Option | Description |
|---|---|
| **Entry Cost** | Gil each player pays to take a turn |
| **Boosted Pot** | Extra Gil added by the venue on top of trade revenue |
| **Host roll /dice X** | Dice size the host rolls (default 10) |
| **Auto Win Count** | When on, a player who reaches the target round count wins instantly |
| **Allow Multiple Winners** | When on, everyone tied on the top streak shares the pot; when off, the first player to reach the top streak wins outright |
| **Trades to Pot (%)** | Portion of trade revenue that feeds the pot; the remainder is held back for the venue |

### Guess Detection

The player's call is picked up automatically from party or alliance chat: any message containing `h`, `high`, or `higher` registers a Higher guess, and `l`, `low`, or `lower` registers Lower. The host can also click the **Higher** / **Lower** buttons directly, or use **Ask Guess** to prompt the player for their call.

### Statistics Panel

The Statistics panel at the bottom of the Game tab shows the live pot and session figures:

| Row | Description |
|---|---|
| **Total Pot** | Boosted Pot + the configured share of trade revenue, split between all winners; the **Announce Pot** button shouts it on demand |
| **Boosted Pot** | Extra Gil added by the venue on top of trade revenue |
| **Taken in Trades** | Total Gil collected from players this session |
| **Kept from Trades** | Portion of trade revenue held back for the venue (shown only when Trades to Pot is below 100%) |
| **Players Played** | Number of players who have taken a turn this session |
| **Highest Rounds** | The best streak reached so far this session |
| **Currently Winning** | The player (or players) on top of the leaderboard |

**Trades to Pot (%)**, set on the door, controls how much of the trade revenue feeds the pot; whatever is not fed to the pot shows as Kept from Trades.

### Automated Chat

Higher/Lower includes fully customisable message templates with toggleable auto-send:

| Template | Trigger |
|---|---|
| Rules | Manually via **Send Rules** on the Game tab |
| Request Gil | Manually from the Game tab |
| Let's Play | When payment is verified (auto-send toggle) |
| Ask Guess | After each roll while awaiting the player's guess (auto-send toggle) |
| Announce Score | When a turn ends and the player is not in the lead (auto-send toggle) |
| Announce Lead | When a turn ends and the player is in the lead (auto-send toggle) |
| Win Shout | On the winner screen, includes the winner's share of the pot |
| Announce Pot | Posts the current pot on demand |

All outbound messages are queued with a one-second gap between each to stay within FFXIV's chat rate limits.

### Winner Payout

When the session finishes, the winner screen shows each winner's share of the pot. The host can **Announce Winner**, **Trade Winner** to pay them by hand, or use **Auto Payout** to send their share automatically. A progress bar tracks how much of each winner's share has been paid out.

### Screenshots

> **Session Tab (Pre-Session)**
>
> ![Session Tab (Pre-Session)](MiniGamesEmporium/Images/Screenshots/HL-session.png)

---

> **Game Tab: Select Player**
>
> ![Game Tab: Select Player](MiniGamesEmporium/Images/Screenshots/HL-lobby.png)

---

> **Game Tab: In Play**
>
> ![Game Tab: In Play](MiniGamesEmporium/Images/Screenshots/HL-game.png)

---

> **Game Tab: Turn Complete**
>
> ![Game Tab: Turn Complete](MiniGamesEmporium/Images/Screenshots/HL-game-over.png)

---

> **Session Complete: Winner Payout**
>
> ![Session Complete: Winner Payout](MiniGamesEmporium/Images/Screenshots/HL-winner.png)

---

## Commands

| Command | Description |
|---|---|
| `/mge` | Open the main plugin window |
| `/minigamesemporium` | Alias for `/mge` |
| `/mgeconfig` | Open directly to the Settings tab |

---

## How to Install Mini Games Emporium

1. Type `/xlsettings` in the in-game chat to open the Dalamud settings window.
2. Go to the **Experimental** tab.
3. Paste this link into the **Custom Plugin Repositories** field at the bottom:

   `https://puni.sh/api/repository/oof-games`

4. Click the `+` button, ensure the repository is set to **Enabled**, and click **Save and Close**.
5. Type `/xlplugins`, search for **Mini Games Emporium**, and click **Install**.
