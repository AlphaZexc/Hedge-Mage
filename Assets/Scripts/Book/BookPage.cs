using UnityEngine;
using UnityEngine.UI;

// This simple script just holds the references for the images on a single bestiary page.
public class BookPage : MonoBehaviour
{
    [Tooltip("The Image component for the left-side page (e.g., the creature portrait).")]
    public Image leftImage;

    [Tooltip("The Image component for the right-side page (e.g., the description image).")]
    public Image rightImage;

    // A public method so the main controller can give this page its data.
    public void PopulatePage(BestiaryEntry data)
    {
        if (data != null)
        {
            if (leftImage != null) leftImage.sprite = data.leftPageImage;
            if (rightImage != null) rightImage.sprite = data.rightPageImage;
        }
    }
}