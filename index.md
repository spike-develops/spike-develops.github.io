---
layout: splash
sitemap: true
header:
    overlay_image: /assets/files/DameWelcome.jpg
    overlay_filter: linear-gradient(rgba(0, 255, 255, 0.5), rgba(0, 0, 0, .7))

tagline: "Engineer with an affinity for atmospheric projects"

feature_row_COP:
  - image_path: /assets/files/CultofPersonality_Coverart_Submission_Logo.jpg
    alt: "Cop library hero"
    title: "Cult of Personality"
    excerpt: 'Lead network and gameplay Engineer'
    url: /cult-of-personality/
    btn_label: "More Info"
    btn_class: "btn--primary"

feature_row_Articles:
  - title: "Technical Articles"

  - image_path: /assets/files/SpelunkyCam/SpelunkyExampleComplete.gif
    alt: "Spelunky Transition Gif"
    title: "Spelunky Transition"
    excerpt: How I recreated Spelunky 2's transition system
    url: /articles/spelunking-for-camera-transitions/
    btn_label: "Read Article"
    btn_class: "btn--primary"
  - image_path: /assets/files/HSM/TorchHSM.png
    alt: "Torch HSM"
    title: "Networked HSMs"
    excerpt: Netcode with hierarchical state machines
    url: /articles/networked-hsms/
    btn_label: "Read Article"
    btn_class: "btn--primary"
---

{% include feature_row id="feature_row_COP" type="left" %}

<div class="feature__wrapper">
<!-- sort them manually because github is 3.9 and sorting is 4 -->

{% assign projects = site.categories["projects"] | sort: "order_priority", "last" %}
<!-- iterate over the first two items-->
{% for post in projects limit: 2 %}
    {% include custom_feature_single.html %}
{% endfor %}

<!-- super hacky and badn -->
{% assign post = site.pages[5] %}
{% include custom_feature_single.html %}
</div>

{% include feature_row id="feature_row_Articles" %}