# EVIDENCE PACK (three research agents, 2026-10-12)

## COMPETITORS & ALTERNATIVES: evidence only (research date 2026-10-12)

### Home Assistant / Nabu Casa (dominant incumbent)
- Home Assistant Green is $199 / €179 MSRP. Rockchip RK3566 CPU, 4 GB RAM, 32 GB eMMC, 1.7 W idle → https://www.home-assistant.io/green/
- Green price rose $30 (about 23%) to $199, announced 2026-04-27. Nabu Casa said component cost "nearly doubled" since 2023 → https://www.matteralpha.com/news/home-assistant-green-price-hiked-here-s-why
- Voice Preview Edition (Voice PE) is $69 MSRP / €59. Resellers about $59 → https://www.home-assistant.io/voice-pe ; https://www.cnx-software.com/2024/12/20/59-voice-preview-edition-adds-an-offline-smart-speaker-to-your-home-assistant-server
- Green plus one Voice PE is therefore about $268 before tax and shipping (arithmetic from the two MSRPs).
- Voice PE supports fully local operation "provided your language is supported". Focused local mode (Speech-to-Phrase) handles only predefined home-control sentences → https://www.home-assistant.io/voice-pe
- Full local speech-to-text via Whisper Base needs hardware "at least Intel N100-class". The Green (ARM, 4 GB) is below that → https://www.home-assistant.io/voice-pe
- Voice PE can use Google, OpenAI, Anthropic, or local LLMs through Home Assistant. Home Assistant itself calls AI in the smart home "experimental" → https://www.home-assistant.io/voice-pe
- "Year of the Voice" began 2023. Stated goal: a local, open, private voice assistant. LLMs plug in through Ollama or cloud providers. Assist is the non-AI agent for predefined commands → https://www.home-assistant.io/blog/2025/09/11/ai-in-home-assistant
- Home Assistant Cloud (now "Link") costs $6.50/month or $65/year, optional. Adds remote access, backups, cloud STT/TTS, Alexa/Google voice → https://www.nabucasa.com/pricing/
- Home Assistant is free and self-hostable, so the "no subscription, local" claim is already available from the incumbent → https://www.home-assistant.io/voice-pe
- Reported stock-outs of Voice PE at resellers (AmeriDroid showed pre-order). Not a sales figure → https://ameridroid.com/collections/nabu-casa

### Preconfigured-box resellers
- Generic N100 mini PC (16 GB, 512 GB) with Home Assistant OS preinstalled listed at about $288 on a Russian-language marketplace. No LLM claim → https://alaniazapoved.ru/products/home-assistant-pulcro-tk-qbe-mini-pc-n100-cpu-16gb-lpddr5-ra/205846931
- N100 mini-PC prices rose with the DRAM shortage, from about $125-145 to about $250-330 for a Beelink S12 Pro → https://computingforgeeks.com/best-mini-pc-home-assistant/

### Other hubs
- Hubitat C-8 Pro lists at $179.95 (third-party guide dated 2026-07). Processes locally → https://www.promptquorum.com/es/smart-home/best-smart-home-hubs-2027 ; https://homey.app/en-ae/wiki/homey-pro-vs-hubitat/ (vendor comparison, biased)
- Homey Pro is a one-time €399 (Homey's page) or $449 (third-party). Local, no mandatory subscription → https://homey.app/en-ae/wiki/homey-pro-vs-hubitat/ ; https://www.promptquorum.com/es/smart-home/best-smart-home-hubs-2027
- Futurehome went bankrupt May 2025, then imposed a 1,188 NOK (about $117) annual subscription from 2025-06-26. Without it users lose device control. Documented hardware-without-subscription failure → https://www.techdirt.com/?p=511300 ; https://tech.slashdot.org/story/25/07/28/2211238

### Big-tech voice and LLM
- Alexa+ is $19.99/month, or free with Prime ($14.99/month or $139/year) → https://www.techradar.com/home/smart-home/alexa-plus-subscription-pricing-how-much-it-costs
- Google Home Premium (replacing Nest Aware) is $10/month or $20/month. Google Home Speaker is $99 → https://www.channelnews.com.au/google-takes-on-amazon-with-gemini-powered-smart-home-overhaul/
- Apple's smart-home display ("HomePad") rumored at about $350, 7-inch. Slipped to fall 2026 because Siri is not ready; has slipped several times. Not announced → https://www.macworld.com/article/2660287/siri-woes-reportedly-push-apples-smart-home-hub-into-2026.html ; https://9to5mac.com/2025/07/11/new-homepad-product-gets-fresh-launch-timing-update/

### Premium channel
- Josh.ai is sold through installers. Software plans start at $30/month MSRP, with a lifetime one-time option → https://josh.ai/stories/joshai-debuts-scalable-software-plans-for-every-project
- Integrator blog estimates Josh Micro hardware at $500-$800 and a luxury install at $3,000-$10,000. Competitor's opinion → https://restrepoinnovations.com/blog/josh-ai-for-crestron.html

### Open-source voice projects
- Willow: ESP32-S3 voice platform. Willow Inference Server repo (GPU-oriented, CPU "significantly slower") last push May 2024. Activity unclear → https://github.com/toverainc/willow-inference-server
- Rhasspy 3 described by one blogger as stalled. Wyoming (Nabu Casa, 2023) is the modular voice protocol HA uses. Piper TTS repo reportedly archived 2025-10-06 (third-party claim) → https://sumguy.com/home-assistant-voice-wyoming-vs-rhasspy/ ; https://dev.to/kunal_d6a8fea2309e1571ee7/local-ai-voice-assistant-stack-2026-whisper-piper-ollama-wired-together-572l

### Dead prior attempts
- Mycroft: ran out of money early 2023. Told Mark II backers it could not fulfil remaining rewards; criticized for selling 100+ units on eBay at $499. OpenVoiceOS and NeonAI continued the community → https://www.opensourceforu.com/2023/02/open-source-virtual-assistant-project-mycroft-is-cash-strapped/ ; https://blog.openvoiceos.org/posts/2023-02-15-a-brief-history-of-open-voice-os ; https://www.openvoiceos.org/about
- Snips: local, privacy-focused voice platform. Sonos acquired it for $37.5M Nov 2019 and closed public access to the Snips Console within about a week. Sonos CEO: "We are not building an ask-anything assistant." → https://venturebeat.com/business/sonos-acquires-privacy-conscious-ai-startup-snips-for-37-5-million ; https://home-assistant.io/blog/2019/12/03/sonos-shutting-down-local-voice-option-snips

### Small-vendor local-AI boxes (closest analogues)
- Useful Sensors "AI in a Box" (offline LLM, RK3588S NPU) sold at $299 on Crowd Supply. Raised $46,041 against a $30,000 goal. Last update: hardware leaving the factory for the Mouser warehouse → https://www.crowdsupply.com/useful-sensors/ai-in-a-box ; https://www.crowdsupply.com/useful-sensors/ai-in-a-box/updates/production-complete
- CrankGPT (June 2026): hand-cranked offline AI box on an SBC, roughly 1B-parameter models → https://boingboing.net/2026/06/16/crankgpt-is-an-offline-ai-box-for-the-apocalypse.html
- Raspberry Pi 5 (8 GB) local voice pipelines reportedly take 5-8 s end-to-end, vs 1-2 s on a desktop GPU (third-party guide) → https://www.promptquorum.com/es/power-local-llm/build-local-voice-assistant-2026

### INSUFFICIENT DATA (check manually)
- Any commercially successful local-LLM home appliance from a small vendor. None found. Check Kickstarter, Indiegogo, Crowd Supply, r/homeassistant, r/LocalLLaMA.
- Nabu Casa unit sales and revenue for Green, Yellow, Voice PE. Check Nabu Casa / Open Home Foundation annual reports and analytics.home-assistant.io.
- Home Assistant Yellow's current price and whether discontinued.
- Current official Home Assistant "Year of the Voice" 2026 roadmap.
- Mycroft Mark II final delivered unit count, and OVOS/Neon commercial traction.
- Current Willow maintenance status.
- Official Hubitat, Homey, SmartThings US prices; Amazon/Google local-processing claims. Vendor pages not fetched.
- Josh.ai official hardware pricing, dealer margins, unit volume.
- Whether Apple's home hub has launched by 2026-10-12 and its price.
- Actual Qwen 2.5 3B and Gemma 4 E4B performance on a Ryzen 5 3550H.


## Demand-signal evidence (angle: demand). Tooling note: WebSearch returned summaries, not raw quotes; only quotes the tool relayed are included. Coverage was thin (about 8 searches, 1 fetch). Reddit and HN threads were mostly not surfaced.

### Verified findings
- Home Assistant has 699,274 active installations (opt-in analytics, so a floor) → https://analytics.home-assistant.io/ . The page showed no Assist/voice usage figures.
- Amazon removed the "do not send voice recordings" local-processing option from Echo devices via automatic update, because Alexa+ can't run locally. The Open Home Foundation's framing: "Alexa+ cannot run locally, so Amazon has decided to remove this feature with an automatic update." → https://newsletter.openhomefoundation.org/ai-voice-control-at-what-cost/ . This is a vendor-adjacent source (the HA side), not neutral.
- Alexa+ is reported at $20/month, free with Prime, per a Home Assistant blog. Not verified against Amazon → https://newsletter.openhomefoundation.org/ai-voice-control-at-what-cost/
- Insteon went offline in April 2022 without notice, leaving hubs unusable. The status page still read "All Services Online", and users demanded a class action. Some moved to Home Assistant.
  - https://hackaday.com/tag/insteon/
  - https://www.theregister.com/2022/04/19/insteon_cloud_shutdown/
  - https://www.fiercesensors.com/iot-wireless/insteon-iot-users-ghosted-upset-over-apparent-shutdown
  - Fierce Sensors headline: "consumers are like guinea pigs" → https://www.fiercesensors.com/sensors/insteons-apparent-shutdown-shows-consumers-are-guinea-pigs
- Fierce Sensors headline: "Insteon's shutdown is shameful, but won't slow smart home growth". Market kept growing despite shutdowns → https://www.fiercesensors.com/sensors/insteons-shutdown-shameful-wont-slow-smart-home-growth
- Privacy paradox, surveys:
  - Capgemini 2019: about half of 12,000 respondents (US/UK/FR/DE) owned a voice device, up from 25% in Nov 2017, while 52% worried the assistant was listening to private conversations → https://www.bloomberg.com/news/articles/2019-09-05/users-worry-about-smart-speaker-privacy-keep-on-buying-them
  - Voicebot 2020: privacy cited by 1 in 3 consumers as a reason to avoid smart speakers, double the figure two years earlier → https://voicebot.ai/2020/05/11/privacy-concerns-rise-significantly-as-1-in-3-consumers-cite-it-as-reason-to-avoid-smart-speakers/
  - 2017 NPR data: price ranked above privacy. Non-owners: 60% said the devices are too expensive → https://feeds2.benzinga.com/tech/17/10/10194340/the-top-reason-some-wont-buy-a-smart-speaker-price-not-privacy
  - University of Michigan (Florian Schaub): people are "resigned to giving up their privacy" → https://record.umich.edu/?p=46272
  - UK Echo/Google Home users study: "privacy pragmatism or privacy cynicism" → https://biopen.bi.no/bi-xmlui/handle/11250/2754878
  - 2022 study of 360 users confirmed the personalisation–privacy paradox (Electronic Commerce Research and Applications; no direct URL).
  - All of these are 2017–2022 surveys of cloud smart speakers. Nothing found from 2025–26.
- Non-technical household members and Home Assistant (anecdotal):
  - HA community poster built a room-first UI because dashboards were "lots of entities, lots of things I understand because I built them", while wife and guests "just want to turn the lights on" → https://community.home-assistant.io/t/a-room-first-home-assistant-frontend-for-the-rest-of-the-household/1020977
  - A husband says his wife uses HA only to arm/disarm the alarm and sees it as "a gimmick" → https://community.home-assistant.io/t/fellow-husbands-unite/66029
  - r/homeautomation thread "My partner refuses to open Home Assistant" → https://rl.klein.ruhr/r/homeautomation/comments/1v3db1l/my_partner_refuses_to_open_home_assistant/oz2x7y2/?context=3
  - Lemmy commenter: HA is "incredibly complicated to set up"; after repeated breakage from updates went back to manual switches. Another calls setup "stupidly simple" → https://lemmy.kde.social/comment/3419605
  - SmartThings community: a wife with a doctorate found the interface baffling → https://community.smartthings.com/t/non-technical-people-using-smartthings/351
- Local voice is paid for today, at modest price points, with no ready-made appliance:
  - Voice Preview Edition retail $69 / €59 at launch (Dec 2024) → https://www.matteralpha.com/review/home-assistant-voice-preview-edition-review
  - Sold out / out of stock at several retailers (The Pi Hut, Pimoroni EU); a Lemmy user said the vendor sold out in 21 minutes. Anecdote; may reflect limited supply → https://thepihut.com/collections/all/products/home-assistant-voice-preview-edition , https://lemmy.linuxuserspace.show/comment/2108761
  - Home Assistant Green raised to $199 / €179 → https://www.matteralpha.com/news/home-assistant-green-price-hiked-here-s-why
  - Vendor blog estimates local voice at ~$200 server plus $80 per voice satellite, no subscription → https://joinhomeshift.com/home-assistant-voice-control/ (promotional). Notes local voice means more setup and slightly slower responses.
- Telegram as a voice interface:
  - HA has a native Telegram integration and a send_voice action → https://www.home-assistant.io/integrations/telegram_chatbot , https://www.home-assistant.io/actions/telegram_bot.send_voice/
  - DIY projects exist: hassgram (https://hub.docker.com/r/lordraw/hassgram), n8n workflow using Telegram, Whisper and Gemini (https://n8n.io/workflows/8411-voice-and-text-control-for-home-assistant-using-telegram-whisper-and-gemini/), https://www.smartlab.at/?p=7548
  - No built-in HA path for voice-note-to-action found. Telegram as a house interface exists as a hobbyist niche; no evidence of mainstream demand.

### INSUFFICIENT DATA (check manually)
- Voice PE and Green unit sales, and Nabu Casa Cloud subscriber count. Check the Open Home Foundation annual report and HA state-of-the-union posts.
- Direct Reddit, HN and HA-forum quotes on "I'd pay for a box that just works locally" or "local voice assistant".
- Chamberlain MyQ, Wink subscription, Google Nest Hub changes, and any 2025–26 cloud shutdowns. Not covered.
- Alexa+ complaints in user words, and Alexa+ adoption.
- Josh.ai install base. Not searched.
- Market reports on privacy-focused smart home (size and growth). Not searched.
- Current (2024–26) surveys on whether privacy or "local AI" drives purchase. Only 2017–2022 data found.
- Google Trends, and GitHub star growth for home-llm, Willow, OVOS and HA Assist. Not retrieved.
- Whether mainstream consumers treat "local AI" as a buying criterion. No evidence either way.
- Any existing preconfigured local-LLM home appliance competitors and their sales. Not searched.


## Findings: Distribution & Economics (evidence only, as of 2026-10-12)

Caveat: the search tool returned summaries, not full pages. Every figure is only as good as the linked source. Primary pages were not fetched.

### Crowdfunding / distribution
- Kickstarter tech: 5,388 of 27,360 campaigns (19.6%) funded. Old snapshot → https://soundandvision.com/node/116509
- CircuitHub analysis: hardware Kickstarter failure 42%, plus 9% cancelled/suspended → https://circuithub.com/post/how-to-deliver-your-kickstarter-hardware
- 2013 review: 84% of top-50 funded projects missed ship date; only 8 shipped on time. Biggest campaigns only → https://kiledjian.com/2013/06/27/of-kickstarter-projects-ship-late.html ; https://hothardware.com/news/most-heavilyfunded-kickstarter-projects-miss-their-ship-dates-cant-handle-success
- Chalmers thesis on delayed hardware projects: partner quality, developer knowledge, uncertainty. No rate → https://odr.chalmers.se/items/f2a5c510-eeef-4d36-aef0-85c5118e5aea
- Silk Labs Sense raised over $100k on day one at $249 (2016). Shipping outcome not stated → https://www.crowdfundinsider.com/?p=81952 (URL unconfirmed)
- Atmos Home (privacy-focused local voice hub) raised over $1.2M incl. $1M from Luminance Brands → https://www.cepro.com/news/atmos-home-debuts-privacy-focused-voice-control-hub/26512/
- Tindie: sellers pay 5% fee plus 3-4% payment processing → https://pitchbook.com/profiles/company/56179-54
- Tindie: median orders per product were 4 (2018). Only 49.9% of 10,152 products had been ordered at least once → https://blog.tindie.com/?p=8731
- Tindie: estimated total site sales about $1.06M in August 2024, AOV $150-175. Third-party estimate → https://gripsintelligence.com/insights/retailers/tindie.com
- Tindie: one small seller reports 2-3 orders/week across eBay, Amazon, Tindie, Shopify, "enough to cover costs". Anecdotal → https://discuss.tindie.com/t/things-to-help-small-volume-sellers/594

### Hardware cost / BOM
- Retail Ryzen 5 3550H mini PCs (Bosgame E2, GenMachine): 16GB/512GB at $259.99 (Slickdeals, undated) and $268.99 (Newegg) → https://slickdeals.net/f/19758498-bosgame-e2-mini-pc-ryzen-5-3550h-16gb-ddr4-512gb-ssd-win-11-pro-259-99 ; https://www.newegg.com/p/9SIAFVFE7U6250
- Wholesale: Global Sources supplier lists a 3550H/32GB unit at $274 for 1-99 units → https://www.globalsources.com/product/mini-pc_1224414489f.htm
- The 3550H is a 2019 mobile part. Minisforum DeskMini DMAF5 and Bosgame E2/E4 are the models that appear → https://www.notebookcheck.net/MINISFORUM-DeskMini-DMAF5-Mini-PC-with-an-AMD-Ryzen-5-3550H-processor-is-now-shipping.500958.0.html
- DRAM/NAND shortage: Minisforum raised prices on RAM/SSD configurations from 2025-11-04 → https://borecraft.com/2025/10/31/mini-pc-maker-minisforum-to-hike-prices-on-all-models-with-ssds-and-dram-cites-significant-increase-in-our-overall-costs-barebones-models-not-affected-suggesting-change-caused-by-memory/
- IDC: shortage may last into 2027, OEM hikes 15-20% → https://www.techradar.com/pro/experts-warn-pc-prices-could-rise-even-more-in-2026-as-ram-costs-set-to-soar-higher
- Beelink S13 (N100) went from $169 (Aug 2025) to $269 on Amazon → https://www.starryhope.com/minipcs/dram-price-spike-mini-pc-raspberry-pi-2026/
- Home Assistant Green rose to $199/€179. Nabu Casa says component cost nearly doubled since 2023 → https://www.matteralpha.com/news/home-assistant-green-price-hiked-here-s-why

### Compliance / fees / returns
- FCC SDoC for PCs (unintentional radiators): responsible party is manufacturer, assembler or importer, must have a US presence → https://compliancegate.com/?p=7230
- A modifier of a certified device becomes responsible for its compliance (source is about intentional radiators; may not map to a non-radio PC) → https://compliancetesting.com/fcc-testing-and-verification/
- Sources do not say outright that reselling an unmodified certified PC is exempt. Inference only. Check FCC KDB 896810.
- Amazon referral fee for Consumer Electronics is 8%, $0.30 minimum. Sources conflict on whether PCs are 6% or 8% → https://www.repricer.com/?p=1593 ; https://eightx.co/blog/average-amazon-referral-fee-by-category
- FBA fulfillment fees rose January 2026. 3.5% fuel surcharge on FBA fees from 2026-04-17 → https://jarvio.io/blog/amazon-fee-changes-2026
- Smart-home returns, Parks Associates: about 3% of US broadband households returned a smart-home device in 12 months. Among smart video doorbell owners, 16% reported a return → https://www.parksassociates.com/blogs/home-systems-and-controls/product-returns-and-the-smart-home7
- Parks: 27% of returners cited physical installation difficulty, 22% configuration problems. One in five who found setup "very difficult" returned the device → https://parksassociates.com/blogs/home-systems-and-controls/difficulty-setting-up-smart-home-devices-is-a-significant-driver-of-product-returns4
- Online consumer-electronics return rate about 8-11%. A vendor blog lists smart home at 14.9% (own synthesis) → https://eightx.co/blog/average-electronics-return-rate-benchmarks
- Accenture via Corso: 68% of consumer-electronics returns are "no trouble found"; about 5% involve a real defect → https://www.corso.com/post-purchase-resource-center/electronics-returns-what-every-seller-needs-to-know

### Comparables and alternative models
- Nabu Casa: about 30 employees (Built In), Irvine CA, profitable, no outside investors. Income from Home Assistant Cloud subscriptions and hardware. Home Assistant has over 2M households → https://www.builtinnyc.com/company/nabu-casa ; https://www.builtinnyc.com/job/global-channel-manager/8774614
- Home Assistant Cloud $65/year or $6.50/month (third-party listing) → https://subger.com/en/service/home-assistant-cloud
- Voice PE sold out at several retailers (about $59 / £55.80). No unit counts public → https://thepihut.com/collections/all/products/home-assistant-voice-preview-edition ; https://wholesale.pimoroni.com/products/home-assistant-voice
- Umbrel Home launched at $699 as a plug-and-play server → https://www.nasdaq.com/articles/umbrel-announces-new-umbrel-home-plug-and-play-server
- Start9 Server One about $150 cheaper than Umbrel Home; one comparison lists $563 → https://stacker.news/items/186031
- Start9 Server Pure $800-934 on a partner listing bundled with lifetime support; a third-party retailer lists $451 → https://novasapien.com/product/server-pure/
- Start9's store range roughly $199 to $1,637 → https://stacker.news/items/186031
- Umbrel Home is a one-time-price box with free OS updates. Closest analogue to the brief's model → https://community.umbrel.com/t/introducing-umbrel-home-the-ultimate-home-server-for-self-hosting/12588

## INSUFFICIENT DATA (check manually)
- Nabu Casa revenue and paid-subscriber count. Check Open Home Foundation annual reports and Nabu Casa blog.
- Hubitat revenue and headcount.
- Umbrel Home and Start9 units sold or revenue.
- CasaOS/ZimaBoard and Home Assistant OS monetization.
- Median Kickstarter smart-home raise and fulfilment rate. Figures above are 2013-2015 or all-hardware.
- Current Amazon price of a 32GB 3550H mini PC. Prices above are undated or 16GB; DRAM shortage likely raised them.
- Product liability insurance cost for a small hardware seller. Get quotes.
- Apple $99 and Google $25 developer fees, and effort of maintaining iOS and Android apps.
- Support load per unit; Home Assistant forum volume as proxy.
- Cost of Linux image maintenance and OTA updates.
- Evidence buyers care about a "single stack, fewer packages" claim. None found either way.
- Evidence that small vendors reach $25-50k/yr net in years 1-2. None found.
- Unit math for $25-50k net at $450 retail not computed; needs verified BOM, fee, return and support costs.
