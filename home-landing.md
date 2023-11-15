---
layout: splash
permalink:  /home/

header:
    overlay_color: "#333"

feature_row_COP:
  - image_path: assets\files\CultofPersonality_Coverart_Submission_Logo.jpg
    alt: "Cop library hero"
    title: "Cult of Personality"
    excerpt: 'okay so this is the excerpt `type="left"`'
    url: /cult-of-personality/
    btn_label: "Read More"
    btn_class: "btn--primary"
---

{% include feature_row id="feature_row_COP" type="left" %}

<div class="feature__wrapper">
<!-- sort them manually because github is 3.9 and sorting is 4 -->
{% assign demos = site.demos | sort: "order_priority", "last" %}
<!-- iterate over the first two items-->
{% for post in demos limit: 2 %}
    {% include custom_feature_single.html %}
{% endfor %}
<!-- TODO insert the go look at everything button -->
</div>