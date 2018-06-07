jQuery(document).ready(function () {

////////////////////////////////////////////////////////////////////////////////////
////////////////////// Front Slider /////////////////////////////////////////////
if($('#front-slider') != 0){
    $('#front-slider').royalSlider({
      arrowsNav: false,
      //loop: false,
      keyboardNavEnabled: true,
      controlsInside: false,
      imageScaleMode: 'fill',
      arrowsNavAutoHide: true,
      autoScaleSlider: true, 
      autoScaleSliderWidth: 1200,     
      autoScaleSliderHeight: 650,
      controlNavigation: 'bullets',
      thumbsFitInViewport: false,
      navigateByClick: true,
      startSlideId: 0,
      autoPlay: {
        // autoplay options go gere
        enabled: true,
        pauseOnHover: true,
        delay: 6000
      },
      transitionType:'move',
      globalCaption: false,
      deeplinking: {
        enabled: true,
        change: false
      },
      /* size of all images http://help.dimsemenov.com/kb/royalslider-jquery-plugin-faq/adding-width-and-height-properties-to-images */
      imgWidth: 1200,
      imgHeight: 650
    });
}
////////////////////////////////////////////////////////////////////////////////////
    ////////////////////// Lightbox Gallery /////////////////////////////////////////////
if ($('#login-btn').length > 0) {
    $('#login-btn').magnificPopup({
        type: 'inline',
        midClick: true,
        closeOnBgClick: false,
        callbacks: {
            close: function () {
                document.location = "/";
            }
        }
    });
}
if ($('#signup-btn').length > 0) {
    $('#signup-btn').magnificPopup({
        type: 'inline',
        midClick: true,
        closeOnBgClick: false,
        callbacks: {
            close: function () {
                document.location = "/";
            }
        }
    });
}
  ////////////////////////////////////////////////////////////////////////////////////
////////////////////// Lightbox Gallery /////////////////////////////////////////////

});