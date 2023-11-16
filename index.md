---
layout: splash
sitemap: false
header:
    overlay_color: "#333"
feature_row_COP:
  - image_path: /assets/files/CultofPersonality_Coverart_Submission_Logo.jpg
    alt: "Cop library hero"
    title: "Cult of Personality"
    excerpt: 'Lead network and gameplay engineer'
    url: /cult-of-personality/
    btn_label: "More Info"
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