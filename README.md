# Oops, Rent's Due!
### A co-op life-sim game of housemates in an urban neighborhood, where players must balance money, energy, and social capital to survive through this months' rent payment.

**Course:** DESIGN 6197: Designing Games for Research  
**Team:** Julia Beitel, Edozie Onumonu, Lydia Kim, Michelle Hui  
**Final Submission**

---

## Overview

*Oops, Rent's Due!* is a cooperative mulitplayer Unity game in which players work together to make rent as a group while navigating a neighborhood full of jobs, shops, event cards, and community decisions. Players earn money and network points by visiting job stations, spending at local businesses, and responding to community event cards — all while managing shared resources and group dynamics under the pressure of an upcoming rent deadline.

The game was designed to surface real tensions around urban housing, mutual aid, gentrification, and civic participation through play.

---

## Repository Structure

```
repo/
├── Oops Rent's Due Unity Game/         # Full Unity project source files
├── Team Assets/                        # Supporting design and presentation materials
│   ├── appendix (past iterations)/     # Early layouts, banners, and legacy assets
│   │   ├── layout.png
│   │   ├── layout_snowstorm.png
│   │   ├── result banner lose.png
│   │   ├── result banner win.png
│   │   ├── snowstorm game layout.png
│   │   ├── character design/
│   │   │   ├── p1 v1.png
│   │   │   └── p2 v1.png
│   │   └── station assets/
│   ├── UI elements/                    # Finalized visual assets used for the game
│   │   ├── game background.png
│   │   ├── game board layout.png
│   │   ├── game logo.png
│   │   ├── Instruction card.png
│   │   ├── start card.png
│   │   ├── upgrades card.png
│   │   ├── character design/
│   │   │   ├── p1 v2.png
│   │   │   ├── p2 v2.png
│   │   │   ├── p3 v2.png
│   │   │   └── p4 v2.png
│   │   ├── event cards/
│   │   │   ├── child care event card.png
│   │   │   ├── internet outage event card.png
│   │   │   └── rat infestation event card.png
│   │   └── station assets/             # Final station + station-menu sprites
│   ├── May 4 - Final Presentation.pptx
│   └── Oops Rent's Due!_1.mp4          # Demo trailer
│   └── build         # game build
└── README.md
```

---

## How to Play

After the start and instruction screens, players enter an apartment-upgrade voting phase, then begin live rounds on the neighborhood board. During each round, players move between stations to trade resources, build up shared money/network/energy, and coordinate contributions toward rent. At fixed time intervals, a group event card pauses play and all joined players must vote on one shared choice (B/A/Y) to continue. If players cannot keep up with rent pressure before the final deadline, the team loses; if they successfully cover rent, the team wins.

**Key mechanics:**
- **Apartment Upgrade Vote (Pre-game)** — Players choose one shared apartment upgrade before gameplay starts; if no consensus is reached before the timer, the opportunity is missed.
- **Station Economy** — Different stations convert resources at different rates (e.g., factory favors money output, others trade for network or utility), forcing role specialization and route planning.
- **Rent System** — Rent is a shared group objective tracked live; contributions and shortfalls are visible through HUD feedback.
- **Timed Event Cards** — Event cards interrupt rounds and require unanimous group voting. Choices apply shared resource costs and can push resources into negative values.
- **Deadline Pressure** — Rounds are timer-driven, so teams must balance short-term spending, recovery actions, and rent progress before the final lose condition.

---

## Playtesting Documentation

### Session 1 — April 15, 2026

**Participants:** 4 players (Cornell students)  
**Format:** 2 rounds of gameplay followed by structured post-play interviews

#### What We Observed

Players completed the full game and made rent in approximately **5 minutes**. Contributions to rent were disproportionate — players spent energy differently and visited stations at different rates. Notably, players worked at the factory **3–4x**, compared to only **1–2x** at the art store, even though several players mentioned afterward that they had wanted to pursue network points for new gameplay variety.

#### Hypothesis Check

Going into the session, we expected:

1. **Players would gravitate toward group consensus early on, then interact more opportunistically later** — *Confirmed.* Players naturally defaulted to collaboration and group decision-making, particularly on event cards. One player expressed satisfaction at "coming to your first consensus as a group."

2. **Players would prefer proximity over price in the customization menu** — *Partially confirmed.* One player explicitly chose the big chain grocery store over the deli for its lower prices, but noted feeling "guilty" about not supporting the smaller local store. Price mattered, but players felt genuine tension.

3. **Players would use network points to pay rent rather than chasing money alone** — *Confirmed.* Players successfully utilized network points toward rent, validating it as a meaningful and appealing mechanic.

#### Feedback by Category

**Difficulty & Pacing**
- Network points accumulated too easily — players felt it was not challenging enough to earn them
- Event card decisions lacked tension; one player wished "the quandaries were harder" and found the answers too obvious
- No moments of meaningful struggle — players never felt truly stuck
- Players wanted something to "save up for," like a TV for the apartment — a goal beyond just making rent

**Mechanics Clarity**
- The minus sign (–) used for costs was unclear; players suggested bullet points instead
- Players wanted a visible rent progress bar showing total owed and amount remaining
- Players understood the core station trade-off (factory = more money vs. farmers market/volunteering = network points)

**Social Dynamics**
- Players maintained outward cooperation throughout, but post-play interviews revealed hidden strategies and private motivations
- One player had wanted to spend all the money instead of paying rent, but stayed silent because the group had already agreed on a plan
- Another player had been secretly maxing out factory shifts to stockpile energy, because energy felt so scarce to them
- These "unspoken rules" created interesting social friction that only surfaced in interviews — a sign the game is generating real social dynamics worth designing into more explicitly

#### Interview Questions Used

*Round 1:*
- What were your first impressions when you sat down to play?
- Was the goal (making rent as a group) immediately clear, or did it take a few rounds to click?
- Did the group naturally collaborate, or did you find yourselves competing?
- Did the rent amount feel achievable, stressful in a fun way, or just frustrating?
- Was the function of each building intuitive? Did cost vs. value feel fair?
- Did event cards create interesting interactions?
- Did the game make you think differently about how people navigate housing pressure in real neighborhoods?

*Round 2 (after players were more familiar):*
- Did you come in with a plan this round, and did it hold up?
- Were there resources or jobs you ignored completely?
- Did your group develop any unspoken rules or norms?
- Did knowing the game better make you more or less generous with other players?
- Is there a dynamic — displacement, mutual aid, gentrification — the game still isn't capturing?

#### Key Takeaways for Iteration

- Raise the difficulty of network point accumulation or add a cap per round
- Redesign event cards to have higher-stakes, less obvious answers
- Add a long-term savings mechanic or "win condition" beyond making rent each round
- Replace the minus sign cost notation with clearer visual indicators
- Add a rent progress tracker visible to all players at all times
- Consider designing explicit mechanics around hidden motivations or secret goals to surface the social dynamics that naturally emerged in interviews
