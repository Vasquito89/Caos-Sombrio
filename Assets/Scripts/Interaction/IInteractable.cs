using UnityEngine;


public interface IInteractable
{
    string GetInteractionPrompt();

    void Interact(GameObject interactor);
}
