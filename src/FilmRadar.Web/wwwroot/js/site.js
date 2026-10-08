document.querySelectorAll("img[data-poster]").forEach((image) => {
    const fallback = () => {
        if (!image.src.endsWith("/images/poster-placeholder.svg"))
            image.src = "/images/poster-placeholder.svg";
    };
    image.addEventListener("error", fallback, { once: true });
    if (image.complete && image.naturalWidth === 0) fallback();
});
