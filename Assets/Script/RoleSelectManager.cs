using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class RoleSelectManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown fwDropdown;
    [SerializeField] private TMP_Dropdown mfDropdown;
    [SerializeField] private TMP_Dropdown dfDropdown;

    private PlayerData[] selectedPlayers;
    void Start()
    {
        if(TeamSelectManager.SelectedPlayers==null)
        {
            Debug.Log("選手が選択されていません");
            return;
        }
        selectedPlayers=TeamSelectManager.SelectedPlayers.ToArray();

        SetDropdown();
    }
  
    private void SetDropdown()
    {
        fwDropdown.ClearOptions();
        mfDropdown.ClearOptions();
        dfDropdown.ClearOptions();

        foreach (PlayerData playere in selectedPlayers)
        {
            fwDropdown.options.Add(new TMP_Dropdown.OptionData(playere.playerName));
            mfDropdown.options.Add(new TMP_Dropdown.OptionData(playere.playerName));
            dfDropdown.options.Add(new TMP_Dropdown.OptionData(playere.playerName));

        }
        //初期位置を変更
        fwDropdown.value = 0;
        mfDropdown.value = 1;
        dfDropdown.value = 2;

        //表示更新
        fwDropdown.RefreshShownValue();
        mfDropdown.RefreshShownValue();
        dfDropdown.RefreshShownValue();
    }

    public void ConfirmRole()
    {
        if(fwDropdown.value==mfDropdown.value||
            fwDropdown.value==dfDropdown.value||
            mfDropdown.value==dfDropdown.value)
        {
            Debug.Log("同じ選手を複数の役割に設定できません");
            return;
        }
        TeamDataManager.Instance.forward = selectedPlayers[fwDropdown.value];
        TeamDataManager.Instance.midfielder = selectedPlayers[mfDropdown.value];
        TeamDataManager.Instance.defender = selectedPlayers[dfDropdown.value];

        Debug.Log("FW: " + TeamDataManager.Instance.forward.playerName);
        Debug.Log("MF: " + TeamDataManager.Instance.midfielder.playerName);
        Debug.Log("DF: " + TeamDataManager.Instance.defender.playerName);

        SceneManager.LoadScene("Football");
    }
}
