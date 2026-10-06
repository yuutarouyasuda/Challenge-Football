using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneChange : MonoBehaviour
{
   public void GoToRoleSelect()
    {
        SceneManager.LoadScene("RoleScene");
    }
}
