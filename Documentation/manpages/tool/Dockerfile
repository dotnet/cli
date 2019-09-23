FROM pandoc/core:2.7.2

ENTRYPOINT ["/usr/bin/env"]

RUN apk add py-pip && python -m pip install pandocfilters

CMD /manpages/tool/update-man-pages.sh
